using System.Collections.Concurrent;
using System.Reflection;
using Cimon.Contracts;
using Cimon.Contracts.AppFeatures;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.FeatureManagement;

namespace Cimon.Data.Features;

public class AppFeatureManager : IFeatureDefinitionProvider, IAppInitializer
{
    private readonly IDbContextFactory<CimonDbContext> _contextFactory;
    record FeatureState(FeatureDefinition Definition, bool EnabledGlobally)
    {
        public bool EnabledGlobally { get; set; } = EnabledGlobally;
    }
    private readonly ConcurrentDictionary<string, FeatureState> _features = new();

    private static readonly IEnumerable<FeatureFilterConfiguration> EnabledForAll = new[] {
        new FeatureFilterConfiguration {
            Name = "AlwaysOn"
        }
    };

    public AppFeatureManager(IDbContextFactory<CimonDbContext> contextFactory) {
        _contextFactory = contextFactory;
    }

    private static FeatureState CreateDefinition(string code, FeatureBase featureBase, AppFeatureState? dbState) {
        var def = new FeatureDefinition {
            Name = code,
            RequirementType = RequirementType.All,
        };
        var state = new FeatureState(def, featureBase.Enabled);
        if (dbState is not null) {
            state.EnabledGlobally = dbState.Enabled;
        }
        ActualizeDefinition(state);
        return state;
    }

    private static void ActualizeDefinition(FeatureState state) {
        state.Definition.EnabledFor =
            state.EnabledGlobally ? EnabledForAll : ArraySegment<FeatureFilterConfiguration>.Empty;
    }

    public Task<FeatureDefinition> GetFeatureDefinitionAsync(string featureName) {
        return Task.FromResult(_features.TryGetValue(featureName, out var state)
            ? state.Definition
            : new FeatureDefinition {
                Name = featureName,
                RequirementType = RequirementType.Any
            });
    }

    public IAsyncEnumerable<FeatureDefinition> GetAllFeatureDefinitionsAsync() =>
        _features.Values.Select(x=>x.Definition).ToAsyncEnumerable();

    public Task<IReadOnlyCollection<FeatureModel>> GetAllFeatures() {
        return Task.FromResult<IReadOnlyCollection<FeatureModel>>(_features.Values
            .Select(x => new FeatureModel(x.Definition.Name, x.EnabledGlobally)).ToList());
    }

    public async Task Init(IServiceProvider serviceProvider) {
        var fromCode = new List<FeatureBase>();
        foreach (var assembly in serviceProvider.GetServices<IFeatureAssembly>().Distinct().ToList()) {
            var types = assembly.Assembly.GetTypes().Where(x => x.IsAssignableTo(typeof(FeatureBase)));
            foreach (var type in types) {
                var cacheType = typeof(FeatureInstanceCache<>).MakeGenericType(type);
                if (cacheType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static)
                        ?.GetValue(null) is FeatureBase instance) {
                    fromCode.Add(instance);
                }
            }
        }
        await InitFeatures(fromCode);
    }

    private async Task InitFeatures(List<FeatureBase> fromCode) {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var dbStates = await ctx.FeatureStates.Include(x => x.User).ToListAsync();
        var global = dbStates.Where(x => x.User is null).ToList();
        foreach (var featureBase in fromCode) {
            var dbState = global.FirstOrDefault(x => x.Code.Equals(featureBase.Code));
            var def = CreateDefinition(featureBase.Code, featureBase, dbState);
            _features.TryAdd(featureBase.Code, def);
        }
    }

    private async Task SaveFeatureStateInDb(FeatureState state) {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var row = await ctx.FeatureStates.FirstOrDefaultAsync(x =>
            x.Code == state.Definition.Name && x.User == null);
        if (row == null) {
            row = new AppFeatureState {
                Code = state.Definition.Name
            };
            ctx.FeatureStates.Add(row);
        }
        row.Enabled = state.EnabledGlobally;
        await ctx.SaveChangesAsync();
    }

    public async Task<FeatureModel?> ToggleGlobalValue(string code) {
        if (_features.TryGetValue(code, out var state)) {
            state.EnabledGlobally = !state.EnabledGlobally;
            ActualizeDefinition(state);
            var model = new FeatureModel(code, state.EnabledGlobally);
            await SaveFeatureStateInDb(state);
            return model;
        }
        return null;
    }
}

public record FeatureModel(string Code, bool GlobalValue);