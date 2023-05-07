using System.Collections.Immutable;
using Optional;

namespace Cimon.Data;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;

public class BuildInfoService : IDisposable
{
	private readonly CancellationTokenSource _watchCts;
	private readonly BuildDiscussionStoreService _discussionStore;

	private readonly BehaviorSubject<HashSet<BuildLocator>> _trackedLocators =
		new(new HashSet<BuildLocator>());
	private IObservable<List<BuildInfo>> _buildInfos;

	public BuildInfoService(IOptions<BuildInfoMonitoringSettings> settings,
			IList<IBuildInfoProvider> buildInfoProviders, BuildDiscussionStoreService discussionStore,
			IBuildMonitoringService buildMonitoringService, Func<TimeSpan, IObservable<long>>? timerFactory = null) {
		_discussionStore = discussionStore;
		_watchCts = new CancellationTokenSource();
		timerFactory ??= Observable.Interval;
		_buildInfos = _trackedLocators.CombineLatest(timerFactory(settings.Value.Delay).StartWith(0))
			.SelectMany(async tuple => {
				var (locators, _) = tuple;
				var results = await Task.WhenAll(buildInfoProviders.Select(provider =>
					provider.GetInfo(locators.Where(l => l.CiSystem == provider.CiSystem).ToList())).ToArray());
				var buildInfos = results.SelectMany(x => x).ToList();
				return buildInfos;
			})
			.SelectMany(buildInfos => Observable.FromAsync(async _ => {
				await buildMonitoringService.CheckBuildInfo(buildInfos.ToImmutableArray());
				return buildInfos;
			}))
			.TakeUntil(_ => _watchCts.IsCancellationRequested).Replay().RefCount(1);
	}

	record BuildDiscussionInfo(BuildLocator Locator, BuildDiscussionState State);
	public IObservable<IList<BuildInfo>> Watch(IObservable<IReadOnlyList<BuildLocator>> locators) {
		var trackedLocators = locators.Do(TrackLocators);
		var comments = GetComments(trackedLocators);
		// TODO get current group for each locator using join
		return trackedLocators
			.CombineLatest(_buildInfos)
			.Select(CombineBuildInfos)
			.CombineLatest(comments)
			.Select(tuple => CombineBuildDiscussionState(tuple.First, tuple.Second));
	}

	private IObservable<IReadOnlyCollection<BuildDiscussionInfo?>> GetComments(
			IObservable<IReadOnlyList<BuildLocator>> trackedLocators) {
		var result =
			new BehaviorSubject<Dictionary<string, BuildDiscussionInfo>>(new Dictionary<string, BuildDiscussionInfo>());
		var comments = trackedLocators
			.SelectMany(locators =>
				locators.Select(
					locator => 
						_discussionStore.GetDiscussionService(locator.Id)
							.Select(s => s.State.Select(discussionState => 
								(locator, discussionState)))
							.Switch()
					)).Switch();
		return result.CombineLatest(comments).Do(tuple => {
			var current = tuple.First;
			var (locator, discussionState) = tuple.Second;
			if (discussionState.Status == BuildDiscussionStatus.Closed) {
				if (current.ContainsKey(locator.Id)) {
					current.Remove(locator.Id);
				}
				return;
			}
			if (!current.TryGetValue(locator.Id, out var state)) {
				state = new BuildDiscussionInfo(locator, discussionState);
			} else {
				state = state with { State = discussionState };
			}
			current[locator.Id] = state;
		}).Select(tuple => tuple.First.Values);
	}

	private IList<BuildInfo> CombineBuildDiscussionState(List<BuildInfo> buildInfos, 
		IReadOnlyCollection<BuildDiscussionInfo> buildDiscussionStates) {
		foreach (var buildInfo in buildInfos) {
			var discussionState = buildDiscussionStates.FirstOrDefault(x => x.Locator.Id == buildInfo.BuildId);
			buildInfo.CommentsCount = discussionState?.State.Comments.Count ?? 0;
		}
		return buildInfos;
	}

	private void TrackLocators(IReadOnlyList<BuildLocator> list) {
		var locators = new HashSet<BuildLocator>(_trackedLocators.Value.Concat(list));
		if (!_trackedLocators.Value.SetEquals(locators)) {
			_trackedLocators.OnNext(locators);
		}
	}

	private List<BuildInfo> CombineBuildInfos((IReadOnlyList<BuildLocator> locators, List<BuildInfo> buildInfos) result) {
		var indexes = result.locators.Select(l => l.Id).ToList();
		var infos = result.buildInfos.Where(i => result.locators.Any(l => l.Id == i.BuildId))
			.OrderBy(x => indexes.IndexOf(x.BuildId)).ToList();
		return infos;
	}

	private void Stop() {
		_watchCts?.Cancel();
		_watchCts?.Dispose();
		_buildInfos = null;
	}

	public void Dispose() {
		Stop();
	}
}
