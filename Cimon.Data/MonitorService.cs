using System.Collections.Immutable;

namespace Cimon.Data;

using System.Reactive.Linq;

public class Monitor
{
	public string Id { get; set; }

	public List<BuildLocator> Builds { get; set; } = new();

}

public class MonitorService : IReactiveRepositoryApi<IImmutableList<Monitor>>
{

	private readonly ReactiveRepository<IImmutableList<Monitor>> _state;
	public MonitorService() {
		_state = new ReactiveRepository<IImmutableList<Monitor>>(this);
	}

	public IObservable<IReadOnlyList<Monitor>> GetMonitors() => _state.Items;

	public async Task<Monitor> Add() {
		var monitor = new Monitor {
			Id = "Untitled",
			Builds = new List<BuildLocator>()
		};
		await _state.Mutate(monitors => Task.FromResult(monitors.Add(monitor)));
		// TODO save in db
		return monitor;
	}

	public IObservable<Monitor> GetMonitorById(string? monitorId) {
		return monitorId == null
			? Observable.Empty<Monitor>()
			: GetMonitors().SelectMany(x => x).Where(x => x.Id == monitorId);
	}

	public IObservable<IReadOnlyList<BuildLocator>> GetMonitorBuildsById(string monitorId) {
		return GetMonitors().SelectMany(x => x).Where(x => x.Id == monitorId)
			.Select(m => m.Builds);
	}

	public async Task Save(Monitor monitor) {
		await _state.Mutate(monitors => {
			var existing = monitors.FirstOrDefault(m => m.Id == monitor.Id);
			var newItem = existing != null ? monitors.Replace(existing, monitor) : monitors.Add(monitor);
			return Task.FromResult(newItem);
		});
	}

	public async Task<IImmutableList<Monitor>> LoadData(CancellationToken token) {
		// TODO load from DB
		await Task.Delay(TimeSpan.FromMicroseconds(10), token);
		return MockData.Monitors;
	}
}
