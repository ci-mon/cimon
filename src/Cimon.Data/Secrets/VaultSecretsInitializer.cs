using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;

namespace Cimon.Data.Secrets;

public class VaultSecretsInitializer<TSecrets> :  IConfigureNamedOptions<TSecrets> where TSecrets : class
{
	private readonly string _prefix = typeof(TSecrets).Name.Replace("Secrets", string.Empty);
	private readonly VaultSecrets _vaultSecrets;
	private readonly ILogger _log;
	private readonly IHostEnvironment _environment;

	public void Configure(TSecrets options) => Configure(null, options);

	public void Configure(string? key, TSecrets options) {
		if (_vaultSecrets.Disabled) return;
		if (string.IsNullOrWhiteSpace(_vaultSecrets.Token) && string.IsNullOrWhiteSpace(_vaultSecrets.UserName)) return;
		try {
			ConfigureAsync(options, key).ConfigureAwait(false).GetAwaiter().GetResult();
		} catch (Exception e) {
			_log.LogError(e, "Failed to init secrets {SecretType}", typeof(TSecrets).Name);
		}
	}

	public VaultSecretsInitializer(IOptions<VaultSecrets> vaultSettings,
			ILogger<VaultSecretsInitializer<TSecrets>> log, IHostEnvironment environment) {
		_log = log;
		_environment = environment;
		_vaultSecrets = vaultSettings.Value;
	}

	public TSecrets Get(string key) {
		var secrets = Activator.CreateInstance<TSecrets>();
		Configure(key, secrets);
		return secrets;
	}

	private async Task ConfigureAsync(TSecrets options, string? key) {
		var vaultClient = CreateVaultClient(_vaultSecrets, TimeSpan.FromSeconds(30));
		var path = _vaultSecrets.Path ?? _environment.EnvironmentName;
		var secrets = await vaultClient.V1.Secrets.KeyValue.V2
			.ReadSecretAsync(path: path, mountPoint: _vaultSecrets.MountPoint).ConfigureAwait(false);
		var config = new ConfigurationManager();
		var jsonDataKey =
			secrets.Data.Data.Keys.SingleOrDefault(x => x.Equals(_prefix, StringComparison.OrdinalIgnoreCase));
		if (jsonDataKey != null &&  secrets.Data.Data.TryGetValue(jsonDataKey, out var data) && data is JsonElement jsonVal) {
			if (!string.IsNullOrWhiteSpace(key)) {
				if (jsonVal.TryGetProperty(key, out var prop)) {
					jsonVal = prop;
				} else {
					return;
				}
			}
			var jsonStream = new MemoryStream();
			await using (var writer = new Utf8JsonWriter(jsonStream)) {
				jsonVal.WriteTo(writer);
			}
			jsonStream.Seek(0, SeekOrigin.Begin);
			config.AddJsonStream(jsonStream);
		} else {
			var values = new Dictionary<string, string?>();
			foreach (var item in secrets.Data.Data) {
				string? dataKey = item.Key;
				string fullPrefix = _prefix;
				if (!string.IsNullOrWhiteSpace(key)) {
					fullPrefix = $"{_prefix}.{key}";
				}
				if (!dataKey.StartsWith(fullPrefix, StringComparison.OrdinalIgnoreCase)) continue;
				values[dataKey.Substring(fullPrefix.Length + 1)] = item.Value?.ToString();
			}
			if (values.Count == 0) {
				return;
			}
			config.AddInMemoryCollection(values);
		}
		config.Bind(options);
		_log.LogInformation("{TypeName} initialized from vault: {@Config}", typeof(TSecrets).Name, options);
	}

	internal static IVaultClient CreateVaultClient(VaultSecrets secrets, TimeSpan timeout) {
		IAuthMethodInfo authMethod = string.IsNullOrWhiteSpace(secrets.UserName)
			? new TokenAuthMethodInfo(secrets.Token)
			: new UserPassAuthMethodInfo(secrets.UserName, secrets.Password);
		var vaultClientSettings = new VaultClientSettings(secrets.Url, authMethod) {
			VaultServiceTimeout = timeout
		};
		IVaultClient vaultClient = new VaultClient(vaultClientSettings);
		return vaultClient;
	}
}
