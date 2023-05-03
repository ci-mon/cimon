﻿using System.Collections.Immutable;

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

	public IObservable<IList<BuildInfo>> Watch(IObservable<IReadOnlyList<BuildLocator>> builds) {
		var locators = builds
			.Do(list => {
				var locators = new HashSet<BuildLocator>(_trackedLocators.Value.Concat(list));
				if (!_trackedLocators.Value.SetEquals(locators)) {
					_trackedLocators.OnNext(locators);
				}
			});
		var comments = locators.SelectMany(locatorsList => locatorsList
			.Select(locator =>
				_discussionStore.GetDiscussionService(locator.Id)
					.SelectMany(s => s.State.Select(discussionState=>(locator, discussionState))))
			.Merge());
		return locators
			.CombineLatest(_buildInfos).Select(CombineBuildInfos)
			.CombineLatest(comments.StartWith(default((BuildLocator, BuildDiscussionState))))
			.Select(CombineBuildDiscussionState);
	}

	private List<BuildInfo> CombineBuildDiscussionState(
			(List<BuildInfo> buildInfos, (BuildLocator, BuildDiscussionState) buildDiscussionState) tuple) {
		var buildInfos = tuple.buildInfos;
		if (tuple.buildDiscussionState == default) {
			return buildInfos;
		}
		var buildInfo = buildInfos.Find(x => x.BuildId == tuple.buildDiscussionState.Item1.Id);
		if (buildInfo != null) {
			buildInfo.CommentsCount = tuple.buildDiscussionState.Item2.Comments.Count;
		}
		return buildInfos;
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
