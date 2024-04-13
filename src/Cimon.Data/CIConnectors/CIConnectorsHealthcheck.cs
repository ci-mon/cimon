using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cimon.Data.CIConnectors;

public class CIConnectorsHealthcheck(BuildConfigService buildConfigService) : IHealthCheck
{
	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
			CancellationToken cancellationToken = default) {
		var unhealthy = new List<string>();
		var healthy = new List<string>();
		await foreach (var result in buildConfigService.CheckCiConnectors(cancellationToken)) {
			var status = $"{result.Item1.ConnectorKey}: {result.Item2.Description}";
			if (result.Item2.Status != HealthStatus.Healthy) {
				unhealthy.Add(status);
			} else {
				healthy.Add(status);
			}
		}
		var desc = $"Unhealthy: {string.Join(Environment.NewLine, unhealthy)}{Environment.NewLine}" +
			$"Healthy: {string.Join(Environment.NewLine, healthy)}";
		return !unhealthy.Any()
			? HealthCheckResult.Healthy(string.Join(Environment.NewLine, healthy))
			: HealthCheckResult.Unhealthy(desc);
	}
}
