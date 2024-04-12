using System.Collections.Immutable;
using System.Reactive.Linq;
using Cimon.Data.Common;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Monitor = Cimon.DB.Models.MonitorModel;
using User = Cimon.Contracts.User;

namespace Cimon.Data.Monitors;

public class MonitorService : IReactiveRepositoryApi<IImmutableList<Monitor>>
{
	private readonly IDbContextFactory<CimonDbContext> _contextFactory;

	private readonly ReactiveRepository<IImmutableList<Monitor>> _state;
	public MonitorService(IDbContextFactory<CimonDbContext> contextFactory) {
		_contextFactory = contextFactory;
		_state = new ReactiveRepository<IImmutableList<Monitor>>(this);
	}

	public IObservable<IReadOnlyList<Monitor>> GetMonitors() => _state.Items;

	public async Task<Monitor> Add(User user) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		var userModel = await ctx.Users.SingleOrDefaultAsync(x => x.Id == user.Id);
		var monitor = new Monitor {
			Key = Guid.NewGuid().ToString("D"),
			Title = "Untitled",
			Owner = userModel
		};
		await ctx.Monitors.AddAsync(monitor);
		await ctx.SaveChangesAsync();
		await _state.Refresh();
		return monitor;
	}

	public IObservable<Monitor> GetMonitorById(string? monitorId) {
		return monitorId == null
			? Observable.Empty<Monitor>()
			: GetMonitors().SelectMany(x => x).Where(x => x.Key == monitorId);
	}

	public async Task Save(Monitor monitor) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		ctx.Monitors.Update(monitor);
		await ctx.SaveChangesAsync();
		await _state.Refresh();
	}

	public async Task RemoveBuilds(MonitorModel monitor, IEnumerable<int> buildIds) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		var items = await ctx.MonitorBuilds
			.Where(x => x.MonitorId == monitor.Id && buildIds.Contains(x.BuildConfigId))
			.ToListAsync();
		ctx.MonitorBuilds.RemoveRange(items);
		await ctx.SaveChangesAsync();
		await _state.Refresh();
	}
	public async Task AddBuilds(Monitor monitor, IEnumerable<int> buildIds) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		foreach (var buildConfigId in buildIds) {
			if (await ctx.MonitorBuilds.AnyAsync(x => x.MonitorId == monitor.Id && x.BuildConfigId == buildConfigId)) {
				continue;
			}
			await ctx.MonitorBuilds.AddAsync(new BuildInMonitor {
				MonitorId = monitor.Id,
				BuildConfigId = buildConfigId
			});
		}
		await ctx.SaveChangesAsync();
		await _state.Refresh();
	}

	public async Task<IImmutableList<Monitor>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ctx.Monitors
			.Include(x => x.Owner)
			.Include(x => x.Builds)
			.ThenInclude(x => x.BuildConfig)
			.ThenInclude(x => x.Connector)
			.ToListAsync(cancellationToken: token);
		return result.ToImmutableList();
	}

	public async Task Remove(Monitor monitor) {
		await _state.Mutate(monitors => {
			var existing = monitors.First(m => m.Key == monitor.Key);
			existing.Removed = true;
			return Task.FromResult(monitors.Replace(existing, monitor));
		});
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		ctx.Monitors.Remove(monitor);
		await ctx.SaveChangesAsync();
		await _state.Refresh();
	}

	public async Task Refresh() {
		await _state.Refresh();
	}
}
