using System.Collections.Immutable;
using System.Reactive;

namespace Cimon.Data;

using System.Reactive.Linq;
using System.Reactive.Subjects;

public class Monitor
{
	public string Id { get; set; }

	public List<BuildLocator> Builds { get; set; } = new();

}

public class MonitorService
{
	private readonly ReplaySubject<IImmutableList<Monitor>> _bufferedMonitors;
	private readonly IObservable<IImmutableList<Monitor>> _monitors;

	public MonitorService() {
		_bufferedMonitors = new ReplaySubject<IImmutableList<Monitor>>(1);
		var getDataSubject = new BehaviorSubject<Unit>(Unit.Default);
		var buffer = _bufferedMonitors.Replay().RefCount(2);
		buffer.Subscribe(_ => getDataSubject.OnCompleted());
		_monitors = buffer.Amb(getDataSubject.SelectMany(_ => Observable
			.DeferAsync(async ct => {
				// TODO load from db
				await Task.Delay(TimeSpan.FromMilliseconds(10), ct);
				return Observable.Return(MockData.Monitors);
			})).Replay(1).RefCount().TakeUntil(_bufferedMonitors));
		
	}


	public IObservable<IReadOnlyList<Monitor>> GetMonitors() => _monitors;

	public async Task Add() {
		var monitors = await _monitors.FirstAsync();
		monitors = monitors.Add(new Monitor {
			Id = "Untitled",
			Builds = new List<BuildLocator>()
		});
		_bufferedMonitors.OnNext(monitors);
		// TODO save in db
	}

	public IObservable<Monitor?> GetMonitorById(string monitorId) {
		return GetMonitors().Select(m => m.FirstOrDefault(x => x.Id == monitorId));
	}

	public async Task Save(Monitor monitor) {
		var monitors = await _monitors.FirstAsync();
		var existing = monitors.FirstOrDefault(m => m.Id == monitor.Id);
		monitors = existing != null ? monitors.Replace(existing, monitor) : monitors.Add(monitor);
		_bufferedMonitors.OnNext(monitors);
	}
}
