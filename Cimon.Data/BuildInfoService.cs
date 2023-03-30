namespace Cimon.Data;

using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;

public class BuildInfoService : IDisposable
{
	private readonly BuildInfoMonitoringSettings _settings;
	private readonly IList<IBuildInfoProvider> _watchers;
	private CancellationTokenSource _watchCts;
	private ImmutableList<BuildLocator> _trackedLocators = ImmutableList.Create<BuildLocator>();
	private readonly BehaviorSubject<List<BuildInfo>> _builds = new(new List<BuildInfo>());
	private int _activeWatchers;
	private PeriodicTimer? _timer;

	public BuildInfoService(IOptions<BuildInfoMonitoringSettings> settings, IList<IBuildInfoProvider> watchers) {
		_watchers = watchers;
		_settings = settings.Value;
	}

	public bool IsRunning => _timer != null;

	public IObservable<IList<BuildInfo>> Watch(List<BuildLocator> builds) {
		_trackedLocators = _trackedLocators.AddRange(builds);
		// TODO 1: return info in requested order
		// TODO 2: make builds observable
		return _builds.Select(x => x.Where(i => builds.Any(b => b.Id == i.BuildId)).ToList()).OnSubscribe(() => {
			if (Interlocked.Increment(ref _activeWatchers) == 1) {
				Start();
			}
		}, () => {
			if (Interlocked.Decrement(ref _activeWatchers) == 0) {
				Stop();
			}
		});
	}

	private void Stop() {
		_builds.OnCompleted();
		_timer?.Dispose();
		_timer = null;
	}

	private void Start() {
		if (_timer != null) return;
		var tokenSource = new CancellationTokenSource();
		_watchCts = tokenSource;
		_timer = new PeriodicTimer(_settings.Delay);
		Task.Run(async () => {
			while (await _timer.WaitForNextTickAsync(tokenSource.Token)) {
				var results = await Task.WhenAll(_watchers.Select(x => x.GetInfo(_trackedLocators)).ToArray());
				_builds.OnNext(results.SelectMany(x => x).Distinct().ToList());
			}
		}, tokenSource.Token);
	}

	public void Dispose() {
		_watchCts?.Cancel();
		_watchCts = null;
		_timer?.Dispose();
		_timer = null;
		_builds.Dispose();
	}
}
