using Cimon.Data.CIConnectors;
using Cimon.Data.Secrets;
using Cimon.Data.Users;

namespace Cimon;

public static class DI
{
	public static IHealthChecksBuilder AddHealthChecks(this WebApplicationBuilder builder) {
		var healthChecksBuilder = builder.Services.AddHealthChecks()
			.AddCheck<VaultHealthCheck>("Vault")
			.AddCheck<CIConnectorsHealthcheck>("CiConnector")
			.AddCheck<LdapClient>("LdapClient", tags: new[] { "windows" });
		builder.Services.AddHealthChecksUI(settings => {
				var localAddress = builder.Configuration.GetValue<string>("LOCAL_ADDRESS");
				var healthCheckAddress = "/healthz";
				if (!string.IsNullOrWhiteSpace(localAddress) && Uri.TryCreate(localAddress, UriKind.Absolute,
						out var uri) && Uri.TryCreate(uri, healthCheckAddress, out var healthcheckUri)) {
					healthCheckAddress = healthcheckUri.AbsoluteUri;
				}
				settings.SetEvaluationTimeInSeconds(60 * 5)
					.AddHealthCheckEndpoint("local", healthCheckAddress);
			})
			.AddInMemoryStorage();
		return healthChecksBuilder;
	}
}
