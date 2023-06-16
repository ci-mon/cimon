using System.Collections.Immutable;
using System.Reactive.Linq;
using Cimon.Contracts;
using Cimon.Data.Common;

namespace Cimon.Data.BuildInformation;

public class MonitorService : IReactiveRepositoryApi<IImmutableList<Monitor>>
{

	private readonly ReactiveRepository<IImmutableList<Monitor>> _state;
	public MonitorService() {
		_state = new ReactiveRepository<IImmutableList<Monitor>>(this);
	}

	public IObservable<IReadOnlyList<Monitor>> GetMonitors() => _state.Items;

	public async Task<Monitor> Add() {
		var monitor = new Monitor {
			Id = Guid.NewGuid().ToString(),
			Title = "Untitled",
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

	public async Task Remove(Monitor monitor) {
		await _state.Mutate(monitors => {
			var existing = monitors.First(m => m.Id == monitor.Id);
			existing.Removed = true;
			return Task.FromResult(monitors.Replace(existing, monitor));
		});
	}
}
