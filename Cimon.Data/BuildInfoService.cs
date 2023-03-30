namespace Cimon.Data;

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;

public class BuildInfoService : IDisposable
{
	private readonly BuildInfoMonitoringSettings _settings;
	private readonly IList<IBuildInfoProvider> _buildInfoProviders;
	private CancellationTokenSource _watchCts;

	private readonly BehaviorSubject<HashSet<BuildLocator>> _trackedLocators =
		new(new HashSet<BuildLocator>());
	private IObservable<List<BuildInfo>> _buildInfos;
	private object _buildInfosLocker = new object();
	private int _activeWatchers;

	public BuildInfoService(IOptions<BuildInfoMonitoringSettings> settings, IList<IBuildInfoProvider> buildInfoProviders) {
		_buildInfoProviders = buildInfoProviders;
		_settings = settings.Value;
	}

	public bool IsRunning => _buildInfos != null;

	public IObservable<IList<BuildInfo>> Watch(IObservable<IList<BuildLocator>> builds) {
		return builds.Do(list => {
				var locators = new HashSet<BuildLocator>(_trackedLocators.Value.Concat(list));
				if (!_trackedLocators.Value.SetEquals(locators)) {
					_trackedLocators.OnNext(locators);
				}
			})
			.CombineLatest(GetBuildInfos())
			.Select(CombineBuildInfos)
			.OnSubscribe(() => Interlocked.Increment(ref _activeWatchers), () => {
				if (Interlocked.Decrement(ref _activeWatchers) == 0) {
					Stop();
				}
			});
	}

	private List<BuildInfo> CombineBuildInfos((IList<BuildLocator> locators, List<BuildInfo> buildInfos) result) {
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

	private IObservable<List<BuildInfo>> GetBuildInfos() {
		if (_buildInfos != null)
			return _buildInfos;
		lock (_buildInfosLocker) {
			if (_buildInfos != null)
				return _buildInfos;
			var tokenSource = new CancellationTokenSource();
			_watchCts = tokenSource;
			_buildInfos = _trackedLocators.CombineLatest(Observable.Interval(_settings.Delay))
				.SelectMany(async tuple => {
					var (locators, _) = tuple;
					var results = await Task.WhenAll(_buildInfoProviders.Select(provider =>
						provider.GetInfo(locators.Where(l => l.CiSystem == provider.CiSystem))).ToArray());
					var buildInfos = results.SelectMany(x => x).Distinct().ToList();
					return buildInfos;
				})
				.TakeUntil(_ => tokenSource.IsCancellationRequested).Replay(x=>x);
		}
		return _buildInfos;
	}

	public void Dispose() {
		Stop();
	}
}
