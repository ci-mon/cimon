using System.Collections.Immutable;
using Cimon.Data.Common;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;

namespace Cimon.Data.Monitors;

public class BuildConfigService : IReactiveRepositoryApi<IImmutableList<BuildConfig>>
{
	private readonly IDbContextFactory<CimonDbContext> _contextFactory;
	private readonly ReactiveRepository<IImmutableList<BuildConfig>> _state;
	public BuildConfigService(IDbContextFactory<CimonDbContext> contextFactory) {
		_contextFactory = contextFactory;
		_state = new ReactiveRepository<IImmutableList<BuildConfig>>(this);
	}
	public IObservable<IImmutableList<BuildConfig>> BuildConfigs => _state.Items;

	public async Task<IImmutableList<BuildConfig>> LoadData(CancellationToken token) {
		await using var ctx = await _contextFactory.CreateDbContextAsync(token);
		var result = await ctx.BuildConfigurations.ToListAsync(cancellationToken: token);
		return result.ToImmutableList();
	}
}
