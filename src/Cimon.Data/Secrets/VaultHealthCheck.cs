using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Secrets;

public class VaultHealthCheck : IHealthCheck
{
	private readonly IConfigurationRoot _configuration;

	public VaultHealthCheck(IConfiguration configuration) {
		_configuration = configuration as IConfigurationRoot;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
		CancellationToken cancellationToken = new()) {
		var provider = _configuration.Providers.OfType<VaultConfigurationProvider>().FirstOrDefault();
		if (provider is null) {
			return HealthCheckResult.Unhealthy("VaultConfigurationProvider not found");
		}
		try {
			var client = provider.CreateVaultClient(TimeSpan.FromSeconds(5));
			var status = await client.V1.System.GetHealthStatusAsync();
			if (!status.Initialized) {
				return HealthCheckResult.Unhealthy("Not initialized");
			}
			if (status.Sealed) {
				return HealthCheckResult.Unhealthy("Sealed");
			}
			return HealthCheckResult.Healthy($"Url: {client.Settings.VaultServerUriWithPort}. Loaded keys: {provider.LoadedKeysCount}");
		} catch (Exception e) {
			return HealthCheckResult.Unhealthy(e.Message);
		}
	}
}
