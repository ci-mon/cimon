using System.Collections.Immutable;
using System.Reactive.Linq;
using AngleSharp.Css;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using Cimon.Data.Common;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Monitors;

public class BuildConfigService : IReactiveRepositoryApi<IImmutableList<BuildConfig>>
{
	private readonly IDbContextFactory<CimonDbContext> _contextFactory;
	private readonly IServiceProvider _serviceProvider;
	private readonly ReactiveRepository<IImmutableList<BuildConfig>> _state;
	
	public BuildConfigService(IDbContextFactory<CimonDbContext> contextFactory, IServiceProvider serviceProvider) {
		_contextFactory = contextFactory;
		_serviceProvider = serviceProvider;
		_state = new ReactiveRepository<IImmutableList<BuildConfig>>(this);
	}
	public IObservable<IImmutableList<BuildConfig>> BuildConfigs => _state.Items;

	public async Task<IImmutableList<BuildConfig>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ctx.BuildConfigurations.ToListAsync(cancellationToken: token);
		return result.ToImmutableList();
	}

	
	public async Task RefreshBuildConfigs(CISystem ciSystem) {
		var providers = _serviceProvider.GetServices<IBuildConfigProvider>().Where(x => x.CISystem == ciSystem)
			.ToList();
		var results = await Task.WhenAll(providers.Select(x => x.GetAll()));
		var newItems = results.SelectMany(x => x).ToList();
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		var existingItems = await ctx.BuildConfigurations.Include(x=>x.Monitors).Where(x => x.CISystem == ciSystem).ToListAsync();
		var toRemove = existingItems.ToHashSet();
		foreach (var newItem in newItems) {
			var existing = existingItems.Find(x => x.Key == newItem.Key);
			if (existing == null) {
				existing = new BuildConfig(newItem.Key, ciSystem);
				await ctx.BuildConfigurations.AddAsync(existing);
			} else {
				toRemove.Remove(existing);
				existing.Status = BuildConfigStatus.Ok;
			}
			existing.Props = newItem.Props;
		}
		foreach (var config in toRemove) {
			if (config.DemoState is not null) continue;
			if (config.Monitors.Any()) {
				config.Status = BuildConfigStatus.NotFoundInCISystem;
				continue;
			}
			ctx.BuildConfigurations.Remove(config);
		}
		await ctx.SaveChangesAsync();
		await _state.Refresh();
	}
}
