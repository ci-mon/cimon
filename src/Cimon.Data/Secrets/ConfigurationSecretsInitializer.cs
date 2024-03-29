namespace Cimon.Data.Secrets;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

public class ConfigurationSecretsInitializer<TSecrets> : IConfigureNamedOptions<TSecrets>
	where TSecrets : class
{
	private readonly IConfiguration _configuration;

	public ConfigurationSecretsInitializer(IConfiguration configuration) {
		_configuration = configuration;
	}

	public void Configure(TSecrets options) => Configure(null, options);

	public void Configure(string? name, TSecrets options) {
		IConfigurationSection secretsSection = _configuration.GetSection("Secrets");
		string key = typeof(TSecrets).Name.Replace("Secrets", string.Empty);
		if (!string.IsNullOrWhiteSpace(name)) {
			secretsSection = secretsSection.GetSection(key);
			key = name;
		}
		if (secretsSection.Exists()) {
			secretsSection.Bind(key, options);
		}
	}
}
