using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace Cimon.Data.Secrets;

public class VaultSecretsInitializer<TSecrets> : IConfigureOptions<TSecrets> where TSecrets : class
{
	private readonly string _prefix = typeof(TSecrets).Name.Replace("Secrets", string.Empty).ToLowerInvariant();
	private readonly VaultSettings _vaultSettings;
	private readonly ILogger _log;

	public void Configure(TSecrets options) {
		if (_vaultSettings.Disabled) return;
		try {
			ConfigureAsync(options).ConfigureAwait(false).GetAwaiter().GetResult();
		} catch (Exception e) {
			_log.LogError(e, "Failed to init secrets {SecretType}", typeof(TSecrets).Name);
		}
	}

	public VaultSecretsInitializer(IOptions<VaultSettings> vaultSettings,
			ILogger<VaultSecretsInitializer<TSecrets>> log) {
		_log = log;
		_vaultSettings = vaultSettings.Value;
	}

	private async Task ConfigureAsync(TSecrets options) {
		IAuthMethodInfo authMethod = new TokenAuthMethodInfo(_vaultSettings.Token);
		var vaultClientSettings = new VaultClientSettings(_vaultSettings.Url, authMethod) {
			VaultServiceTimeout = TimeSpan.FromSeconds(30)
		};
		IVaultClient vaultClient = new VaultClient(vaultClientSettings);
		var secrets = await vaultClient.V1.Secrets.KeyValue.V2
			.ReadSecretAsync(path: _vaultSettings.Path, mountPoint: _vaultSettings.MountPoint).ConfigureAwait(false);
		foreach (var property in typeof(TSecrets).GetProperties()) {
			var key = $"{_prefix}.{property.Name.ToLowerInvariant()}";
			if (!secrets.Data.Data.TryGetValue(key, out var value)) continue;
			if (value is not JsonElement jsonElement) continue;
			var propertyValue = jsonElement.Deserialize(property.PropertyType);
			if (propertyValue is null) {
				_log.LogWarning("Value with {Key} deserialized to null", key);
				continue;
			}
			property.SetValue(options, propertyValue);
		}
	}
}
