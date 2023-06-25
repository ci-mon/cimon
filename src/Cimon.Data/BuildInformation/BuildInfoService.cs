using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using Cimon.Data.Discussions;
using Cimon.DB.Models;
using Microsoft.Extensions.Options;

namespace Cimon.Data.BuildInformation;

public class BuildInfoService : IDisposable
{
	private readonly CancellationTokenSource _watchCts = new ();
	private readonly BuildDiscussionStoreService _discussionStore;

	private readonly BehaviorSubject<HashSet<BuildConfig>> _trackedLocators =
		new(new HashSet<BuildConfig>());
	private readonly IObservable<IImmutableList<BuildInfo>> _buildInfos;

	public BuildInfoService(IOptions<BuildInfoMonitoringSettings> settings,
			IList<IBuildInfoProvider> buildInfoProviders, BuildDiscussionStoreService discussionStore,
			IBuildMonitoringService buildMonitoringService, Func<TimeSpan, IObservable<long>>? timerFactory = null) {
		_discussionStore = discussionStore;
		timerFactory ??= Observable.Interval;

		async Task<IImmutableList<BuildInfo>> GetBuildInfos((HashSet<BuildConfig> First, long Second) tuple) {
			var (allBuildConfigs, _) = tuple;
			var results = await Task.WhenAll(buildInfoProviders.Select(provider => {
					var buildInfoQueries = allBuildConfigs
						.Where(l => l.DemoState is null && l.CISystem == provider.CiSystem)
						.Select(x=> new BuildInfoQuery(x))
						.ToList();
					return buildInfoQueries.Any()
						? provider.GetInfo(buildInfoQueries)
						: Task.FromResult((IReadOnlyCollection<BuildInfo>)Array.Empty<BuildInfo>());
				})
				.ToArray());
			var buildInfos = results.SelectMany(x => x).ToList();
			buildInfos.AddRange(allBuildConfigs.Where(x => x.DemoState is not null)
				.Select(x => x.DemoState)!);
			return buildInfos.ToImmutableList();
		}

		_buildInfos = _trackedLocators.CombineLatest(timerFactory(settings.Value.Delay).StartWith(0))
			.SelectMany(GetBuildInfos)
			.SelectMany(buildInfos => Observable.FromAsync(async _ => {
				await buildMonitoringService.CheckBuildInfo(buildInfos);
				return buildInfos;
			}))
			.TakeUntil(_ => _watchCts.IsCancellationRequested).Replay().RefCount(1);
	}

	record BuildDiscussionInfo(BuildConfig Locator, BuildDiscussionState State);
	public IObservable<IList<BuildInfo>> Watch(IObservable<IReadOnlyList<BuildConfig>> buildConfigIds) {
		var trackedLocators = buildConfigIds.Do(TrackLocators);
		var comments = GetBuildDiscussionStates(trackedLocators);
		return trackedLocators
			.CombineLatest(_buildInfos).Select(CombineBuildInfos)
			.CombineLatest(comments).Select(tuple => CombineBuildDiscussionState(tuple.First, tuple.Second));
			// TODO .DistinctUntilChanged() leads to test Watch_SubscribeAndUnsubscribe fail;
	}

	private IObservable<IReadOnlyCollection<BuildDiscussionInfo>> GetBuildDiscussionStates(
			IObservable<IReadOnlyList<BuildConfig>> trackedLocators) {
		var combinedState =
			new BehaviorSubject<List<BuildDiscussionInfo>>(new List<BuildDiscussionInfo>());
		IObservable<(BuildConfig locator, BuildDiscussionState discussionState)> comments = trackedLocators
			.SelectMany(l => l)
			.SelectMany(locator => _discussionStore.GetDiscussionService(locator.Key)
				.Select(ds => ds.State)
				.Switch()
				.Select(state => (locator, state)));
		return combinedState.CombineLatest(comments).Do(tuple => {
				var currentItems = tuple.First;
				var (locator, discussionState) = tuple.Second;
				var currentItem = currentItems.Find(x => x.Locator.Key == locator.Key);
				if (currentItem != null) {
					currentItems.Remove(currentItem);
				}
				if (discussionState.Status == BuildDiscussionStatus.Closed) {
					return;
				}
				if (currentItem != null) {
					currentItem = currentItem with { State = discussionState };
				}
				else {
					currentItem = new BuildDiscussionInfo(locator, discussionState);
				}
				currentItems.Add(currentItem);
			}).Select(tuple => tuple.First)
			.StartWith((IReadOnlyCollection<BuildDiscussionInfo>)new List<BuildDiscussionInfo>());
	}

	private IList<BuildInfo> CombineBuildDiscussionState(List<BuildInfo> buildInfos, 
		IReadOnlyCollection<BuildDiscussionInfo> buildDiscussionStates) {
		foreach (var buildInfo in buildInfos) {
			var discussionState = buildDiscussionStates.FirstOrDefault(x => x.Locator.Key == buildInfo.BuildConfigId);
			buildInfo.CommentsCount = discussionState?.State.Comments.Count ?? 0;
		}
		return buildInfos;
	}

	private void TrackLocators(IReadOnlyList<BuildConfig> list) {
		var locators = new HashSet<BuildConfig>(_trackedLocators.Value.Concat(list));
		if (!_trackedLocators.Value.SetEquals(locators)) {
			_trackedLocators.OnNext(locators);
		}
	}

	private List<BuildInfo> CombineBuildInfos((IReadOnlyList<BuildConfig> locators, 
			IImmutableList<BuildInfo> buildInfos) result) {
		var indexes = result.locators.Select(l => l.Key).ToList();
		var infos = result.buildInfos.Where(i => result.locators.Any(l => l.Key == i.BuildConfigId))
			.OrderBy(x => indexes.IndexOf(x.BuildConfigId)).ToList();
		return infos;
	}

	public void Dispose() {
		_watchCts.Cancel();
		_watchCts.Dispose();
	}
}
