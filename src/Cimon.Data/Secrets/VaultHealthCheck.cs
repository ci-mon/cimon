using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Secrets;

public class VaultHealthCheck : IHealthCheck
{
	private readonly IOptions<VaultSecrets> _vaultSettings;

	public VaultHealthCheck(IOptions<VaultSecrets> vaultSettings) {
		_vaultSettings = vaultSettings;
	}

	public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context,
			CancellationToken cancellationToken = new()) {
		try {
			var client =
				VaultSecretsInitializer<VaultHealthCheck>.CreateVaultClient(_vaultSettings.Value, TimeSpan.FromSeconds(5));
			var status = await client.V1.System.GetHealthStatusAsync();
			if (!status.Initialized) {
				return HealthCheckResult.Unhealthy("Not initialized");
			}
			if (status.Sealed) {
				return HealthCheckResult.Unhealthy("Sealed");
			}
			return HealthCheckResult.Healthy(client.Settings.VaultServerUriWithPort);
		} catch (Exception e) {
			return HealthCheckResult.Unhealthy(e.Message);
		}
	}
}
