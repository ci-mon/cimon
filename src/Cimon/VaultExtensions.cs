using Cimon.Data.Secrets;
using Serilog;
using Serilog.Extensions.Logging;

namespace Cimon;

public static class VaultExtensions
{
	public static WebApplicationBuilder AddVaultConfiguration(this WebApplicationBuilder builder,
			string secretsSection) {
		var settings = builder.Configuration.GetSection($"{secretsSection}:Vault").Get<VaultSecrets>()!;
		var logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<VaultConfigurationProvider>();
		var vaultSource = new VaultConfigurationProvider(secretsSection, settings, builder.Environment, logger);
		((IConfigurationBuilder)builder.Configuration).Add(vaultSource);
		return builder;
	}
}
