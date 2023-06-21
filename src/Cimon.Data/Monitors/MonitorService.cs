﻿using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Runtime.Intrinsics.X86;
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
		};
		await using var ctx = await _contextFactory.CreateDbContextAsync();
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

	public async Task Save(Monitor monitor, IList<BuildConfig> builds) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		ctx.Monitors.Update(monitor);
		var existing = await ctx.MonitorBuilds.Where(x => x.MonitorId == monitor.Id).ToListAsync();
		ctx.MonitorBuilds.RemoveRange(existing);
		foreach (var buildConfig in builds) {
			await ctx.MonitorBuilds.AddAsync(new BuildInMonitor() {
				MonitorId = monitor.Id,
				BuildConfigId = buildConfig.Id
			});
		}
		await ctx.SaveChangesAsync();
		await _state.Refresh();
	}

	public async Task<IImmutableList<Monitor>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ctx.Monitors.Include(x => x.Builds).ThenInclude(x=>x.BuildConfig)
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

}
