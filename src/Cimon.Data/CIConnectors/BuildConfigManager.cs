using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Reactive.Subjects;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.Common;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Optional;

namespace Cimon.Data.CIConnectors;

public class BuildConfigService : IReactiveRepositoryApi<IImmutableList<BuildConfigModel>>
{
	private readonly IDbContextFactory<CimonDbContext> _contextFactory;
	private readonly ReactiveRepository<IImmutableList<BuildConfigModel>> _state;
	private readonly IEnumerable<IBuildConfigProvider> _configProviders;
	
	public BuildConfigService(IDbContextFactory<CimonDbContext> contextFactory,
			IEnumerable<IBuildConfigProvider> configProviders) {
		_contextFactory = contextFactory;
		_configProviders = configProviders;
		_state = new ReactiveRepository<IImmutableList<BuildConfigModel>>(this);
	}

	public IObservable<IImmutableList<BuildConfigModel>> BuildConfigs => _state.Items;

	public async Task<IImmutableList<BuildConfigModel>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ctx.BuildConfigurations.Include(x => x.Connector).ToListAsync(cancellationToken: token);
		return result.ToImmutableList();
	}

	private readonly ConcurrentDictionary<int, ReplaySubject<Option<int>>> _refreshProgress = new();
	public IObservable<Option<int>> GetRefreshProgress(CIConnector connector) {
		return GetProgressPublisher(connector);
	}

	private ReplaySubject<Option<int>> GetProgressPublisher(CIConnector connector) {
		return _refreshProgress.GetOrAdd(connector.Id, _ => new ReplaySubject<Option<int>>(1));
	}

	public async Task RefreshBuildConfigs(CIConnector connector) {
		var progress = GetProgressPublisher(connector);
		progress.OnNext(0.Some());
		await using var ctx = await _contextFactory.CreateDbContextAsync();
		var providers = _configProviders.Single(x => x.CISystem == connector.CISystem);
		var settingsInDb = await ctx.CIConnectorSettings.Where(x => x.CIConnector.Id == connector.Id).ToListAsync();
		var settings = settingsInDb.ToDictionary(x => x.Key, x => x.Value);
		try {
			var newItems = await providers.GetAll(new CIConnectorInfo(settings));
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

	private static async Task Synchronize(CIConnector connector, IReadOnlyCollection<BuildConfig> newItems, ReplaySubject<Option<int>> progress,
		List<BuildConfigModel> existingItems, CimonDbContext ctx, HashSet<BuildConfigModel> toRemove) {
		var totalCount = newItems.Count;
		var current = 0;
		foreach (var newItem in newItems) {
			current++;
			var progressPercents = Convert.ToInt32(current * 1d / totalCount * 100d);
			progress.OnNext(progressPercents.Some());
			var existing = existingItems.Find(x => newItem.Equals(x));
			if (existing == null) {
				existing = new BuildConfigModel(connector, newItem.Key, newItem.Branch, newItem.IsDefaultBranch);
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
