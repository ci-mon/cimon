using System.Collections.Immutable;
using System.Reactive.Linq;
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
		var newItems = (await Task.WhenAll(providers.Select(x => x.GetAll().ToListAsync()))).SelectMany(x => x)
			.ToList();
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		var existingItems = await ctx.BuildConfigurations.Where(x => x.CISystem == ciSystem).ToListAsync();
		foreach (var newItem in newItems) {
			var existing = existingItems.Find(x => x.Key == newItem.Key);
			if (existing == null) {
				existing = new BuildConfig {
					Key = newItem.Key,
					CISystem = ciSystem,
				};
				await ctx.BuildConfigurations.AddAsync(existing);
				continue;
			}
			existing.Props = newItem.Props;
		}
		await ctx.SaveChangesAsync();
	}
}
