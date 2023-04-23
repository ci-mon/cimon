namespace Cimon.Data;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;

public class BuildInfoService : IDisposable
{
	private readonly BuildInfoMonitoringSettings _settings;
	private readonly IList<IBuildInfoProvider> _buildInfoProviders;
	private readonly CancellationTokenSource _watchCts;

	private readonly BehaviorSubject<HashSet<BuildLocator>> _trackedLocators =
		new(new HashSet<BuildLocator>());
	private IObservable<List<BuildInfo>> _buildInfos;

	public BuildInfoService(IOptions<BuildInfoMonitoringSettings> settings,
			IList<IBuildInfoProvider> buildInfoProviders, Func<TimeSpan, IObservable<long>>? timerFactory = null) {
		_buildInfoProviders = buildInfoProviders;
		_settings = settings.Value;
		_watchCts = new CancellationTokenSource();
		timerFactory ??= Observable.Interval;
		_buildInfos = _buildInfos = _trackedLocators.CombineLatest(timerFactory(_settings.Delay).StartWith(0))
			.SelectMany(async tuple => {
				var (locators, _) = tuple;
				var results = await Task.WhenAll(_buildInfoProviders.Select(provider =>
					provider.GetInfo(locators.Where(l => l.CiSystem == provider.CiSystem).ToList())).ToArray());
				var buildInfos = results.SelectMany(x => x).Distinct().ToList();
				return buildInfos;
			}).TakeUntil(_ => _watchCts.IsCancellationRequested).Replay().RefCount(1);
	}

	public IObservable<IList<BuildInfo>> Watch(IObservable<IReadOnlyList<BuildLocator>> builds) {
		return builds
			.Do(list => {
				var locators = new HashSet<BuildLocator>(_trackedLocators.Value.Concat(list));
				if (!_trackedLocators.Value.SetEquals(locators)) {
					_trackedLocators.OnNext(locators);
				}
			})
			.CombineLatest(_buildInfos)
			.Select(CombineBuildInfos);
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
