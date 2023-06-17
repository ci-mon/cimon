using System.Collections.Immutable;
using System.Reactive.Linq;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using Cimon.Data.Common;
using Cimon.Data.Monitors;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Monitor = Cimon.DB.Models.Monitor;

namespace Cimon.Data.BuildInformation;

public class MonitorService : IReactiveRepositoryApi<IImmutableList<Monitor>>
{
	private readonly IDbContextFactory<CimonDbContext> _contextFactory;

	private readonly ReactiveRepository<IImmutableList<Monitor>> _state;
	public MonitorService(IDbContextFactory<CimonDbContext> contextFactory) {
		_contextFactory = contextFactory;
		_state = new ReactiveRepository<IImmutableList<Monitor>>(this);
	}

	public IObservable<IReadOnlyList<Monitor>> GetMonitors() => _state.Items;

	public async Task<Monitor> Add() {
		var monitor = new Monitor {
			Key = Guid.NewGuid().ToString(),
			Title = "Untitled",
			Builds = new List<BuildConfig>()
		};
		await _state.Mutate(monitors => Task.FromResult(monitors.Add(monitor)));
		// TODO save in db
		return monitor;
	}

	public IObservable<Monitor> GetMonitorById(string? monitorId) {
		return monitorId == null
			? Observable.Empty<Monitor>()
			: GetMonitors().SelectMany(x => x).Where(x => x.Key == monitorId);
	}

	public async Task Save(Monitor monitor) {
		await _state.Mutate(monitors => {
			var existing = monitors.FirstOrDefault(m => m.Key == monitor.Key);
			var newItem = existing != null ? monitors.Replace(existing, monitor) : monitors.Add(monitor);
			return Task.FromResult(newItem);
		});
	}

	public async Task<IImmutableList<Monitor>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ctx.Monitors.Include(x => x.Builds)
			.ToListAsync(cancellationToken: token);
		return result.ToImmutableList();
	}

	public async Task Remove(Monitor monitor) {
		await _state.Mutate(monitors => {
			var existing = monitors.First(m => m.Key == monitor.Key);
			existing.Removed = true;
			return Task.FromResult(monitors.Replace(existing, monitor));
		});
	}

}
