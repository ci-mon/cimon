using Cimon.Contracts.CI;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cimon.Contracts.Services;

public class DemoBuildConfigProvider : IBuildConfigProvider
{
	public Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info) {
		var count = int.Parse(info.Settings["Count"]);
		var buildConfigs = Enumerable.Range(0, count).Select(x => new BuildConfig {
			Id = x,
			Name = $"Build {x}",
			Key = $"Build key {x}",
			IsDefaultBranch = x % 2 == 0
		}).ToList();
		return Task.FromResult((IReadOnlyCollection<BuildConfig>)buildConfigs);
	}

	public Dictionary<string, string> GetSettings() {
		return new() {
			["Count"] = 10.ToString()
		};
	}

	public Task<HealthCheckResult> CheckHealth(CIConnectorInfo info) =>
		Task.FromResult(HealthCheckResult.Healthy("demo"));
}
