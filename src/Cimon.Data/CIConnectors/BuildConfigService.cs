using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Runtime.CompilerServices;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.Common;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Optional;

namespace Cimon.Data.CIConnectors;

public class BuildConfigService : IReactiveRepositoryApi<IImmutableList<BuildConfigModel>>
{
	private readonly IDbContextFactory<CimonDbContext> _contextFactory;
	private readonly IServiceProvider _serviceProvider;
	private readonly ReactiveRepository<IImmutableList<BuildConfigModel>> _state;
	
	public BuildConfigService(IDbContextFactory<CimonDbContext> contextFactory,
			IServiceProvider serviceProvider) {
		_contextFactory = contextFactory;
		_serviceProvider = serviceProvider;
		_state = new ReactiveRepository<IImmutableList<BuildConfigModel>>(this);
	}

	private readonly Subject<BuildConfigModel> _singleItemsChanges = new();
	public IObservable<IImmutableList<BuildConfigModel>> BuildConfigs => _state.Items;
	public async IAsyncEnumerable<(CIConnectorInfo, HealthCheckResult)> CheckCiConnectors(
			[EnumeratorCancellation] CancellationToken ct) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(ct);
		foreach (var connector in await ctx.CIConnectors.ToListAsync(ct)) {
			await using var scope = _serviceProvider.CreateAsyncScope();
			var info = await GetConnectorInfo(connector);
			var provider = scope.ServiceProvider.GetRequiredKeyedService<IBuildConfigProvider>(connector.CISystem);
			yield return (info, await provider.CheckHealth(info));
		}
	}

	public IObservable<BuildConfigModel> Get(int id) => _singleItemsChanges
		.Merge(_state.Items.SelectMany(x => x))
		.Where(x => x.Id == id);

	public async Task<IImmutableList<BuildConfigModel>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ConfigureItems(ctx).ToListAsync(cancellationToken: token);
		return result.ToImmutableList();
	}

	private static IIncludableQueryable<BuildConfigModel, CIConnector> ConfigureItems(CimonDbContext ctx) {
		return ctx.BuildConfigurations.Include(x => x.Connector);
	}

	private readonly ConcurrentDictionary<int, ReplaySubject<Option<int>>> _refreshProgress = new();
	public IObservable<Option<int>> GetRefreshProgress(CIConnector connector) {
		return GetProgressPublisher(connector);
	}

	private ReplaySubject<Option<int>> GetProgressPublisher(CIConnector connector) {
		return _refreshProgress.GetOrAdd(connector.Id, _ => new ReplaySubject<Option<int>>(1));
	}

	public async Task<CIConnectorInfo> GetConnectorInfo(CIConnector connector) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		return await GetConnectorInfo(connector, ctx);
	}

	public async Task RefreshBuildConfigs(CIConnector connector) {
		var progress = GetProgressPublisher(connector);
		progress.OnNext(0.Some());
		await using var scope = _serviceProvider.CreateAsyncScope();
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		connector = await ctx.CIConnectors.FindAsync(connector.Id) ??
			throw new InvalidOperationException($"Connector {connector.Id} not found in db");
		var ciConnectorInfo = await GetConnectorInfo(connector, ctx);
		try {
			var provider = scope.ServiceProvider.GetRequiredKeyedService<IBuildConfigProvider>(connector.CISystem);
			var newItems = await provider.GetAll(ciConnectorInfo);
			var existingItems = await ctx.BuildConfigurations.Include(x=>x.Connector).Include(x => x.Monitors)
				.Where(x => x.Connector.Id == connector.Id).ToListAsync();
			var toRemove = existingItems.ToHashSet();
			await Synchronize(connector, newItems, progress, existingItems, ctx, toRemove);
			await ctx.SaveChangesAsync();
			await _state.Refresh();
		} finally {
			progress.OnNext(Option.None<int>());
		}
	}

	private static async Task<CIConnectorInfo> GetConnectorInfo(CIConnector connector, CimonDbContext ctx) {
		var settingsInDb = await ctx.CIConnectorSettings.Where(x => x.CIConnector.Id == connector.Id).ToListAsync();
		var settings = settingsInDb.ToDictionary(x => x.Key, x => x.Value);
		var ciConnectorInfo = new CIConnectorInfo(connector.Key, settings);
		return ciConnectorInfo;
	}

	public async Task Edit(int id, Action<BuildConfigModel> editAction) {
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		var item = await ConfigureItems(ctx).FirstOrDefaultAsync(x => x.Id == id);
		if (item is null) return;
		editAction(item);
		await ctx.SaveChangesAsync();
		await _state.Refresh(true);
		_singleItemsChanges.OnNext(item);
	}

	private static async Task Synchronize(CIConnector connector, IReadOnlyCollection<BuildConfig> newItems, ReplaySubject<Option<int>> progress,
		List<BuildConfigModel> existingItems, CimonDbContext ctx, HashSet<BuildConfigModel> toRemove) {
		var totalCount = newItems.Count;
		var current = 0;
		foreach (var newItem in newItems) {
			current++;
			var progressPercents = Convert.ToInt32(current * 1d / totalCount * 100d);
			progress.OnNext(progressPercents.Some());
			var existing = existingItems.Find(x => newItem.IsSame(x));
			if (existing == null) {
				existing = new BuildConfigModel(connector, newItem.Key, newItem.Name, newItem.Branch, newItem.IsDefaultBranch);
				await ctx.BuildConfigurations.AddAsync(existing);
			} else {
				toRemove.Remove(existing);
				existing.Status = BuildConfigStatus.Ok;
			}
			existing.Props = newItem.Props;
		}
		foreach (var config in toRemove) {
			if (config.Monitors.Any()) {
				config.Status = BuildConfigStatus.NotFoundInCISystem;
				continue;
			}
			ctx.BuildConfigurations.Remove(config);
		}
	}
}
