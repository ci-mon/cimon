using System.Text.Json;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace Cimon.Data.Secrets;

using System.Reflection;

public class VaultSecretsInitializer<TSecrets> :  IConfigureNamedOptions<TSecrets> where TSecrets : class
{
	private readonly string _prefix = ToVaultName(typeof(TSecrets).Name.Replace("Secrets", string.Empty));
	private readonly VaultSettings _vaultSettings;
	private readonly ILogger _log;

	public void Configure(TSecrets options) => Configure(null, options);

	public void Configure(string? key, TSecrets options) {
		if (_vaultSettings.Disabled) return;
		try {
			ConfigureAsync(options, key).ConfigureAwait(false).GetAwaiter().GetResult();
		} catch (Exception e) {
			_log.LogError(e, "Failed to init secrets {SecretType}", typeof(TSecrets).Name);
		}
	}

	public VaultSecretsInitializer(IOptions<VaultSettings> vaultSettings,
			ILogger<VaultSecretsInitializer<TSecrets>> log) {
		_log = log;
		_vaultSettings = vaultSettings.Value;
	}

	public TSecrets Get(string key) {
		var secrets = Activator.CreateInstance<TSecrets>();
		Configure(key, secrets);
		return secrets;
	}

	private async Task ConfigureAsync(TSecrets options, string? key) {
		IAuthMethodInfo authMethod = new TokenAuthMethodInfo(_vaultSettings.Token);
		var vaultClientSettings = new VaultClientSettings(_vaultSettings.Url, authMethod) {
			VaultServiceTimeout = TimeSpan.FromSeconds(30)
		};
		IVaultClient vaultClient = new VaultClient(vaultClientSettings);
		var secrets = await vaultClient.V1.Secrets.KeyValue.V2
			.ReadSecretAsync(path: _vaultSettings.Path, mountPoint: _vaultSettings.MountPoint).ConfigureAwait(false);
		var prefix = string.IsNullOrWhiteSpace(key) ? _prefix : $"{_prefix}.{key}";
		foreach (var property in typeof(TSecrets).GetProperties()) {
			var propertyKey = $"{prefix}.{ToVaultName(property.Name)}";
			if (!secrets.Data.Data.TryGetValue(propertyKey, out var value)) continue;
			if (value is not JsonElement jsonElement) continue;
			var propertyValue = jsonElement.Deserialize(property.PropertyType);
			if (propertyValue is null) {
				_log.LogWarning("Value with {Key} deserialized to null", propertyKey);
				continue;
			}
			property.SetValue(options, propertyValue);
			LogPropertyInitialized(jsonElement, property);
		}
	}

	private void LogPropertyInitialized(JsonElement jsonElement, PropertyInfo property) {
		try {
			string rawText = jsonElement.GetRawText();
			if (jsonElement.ValueKind == JsonValueKind.String && rawText.Length > 2) {
				rawText = rawText.Substring(1, rawText.Length - 2);
			}
			if (jsonElement.ValueKind == JsonValueKind.Array && rawText.Length > 3) {
				rawText = rawText.Substring(2, rawText.Length - 3);
			}
			var length = rawText.Length - 1;
			var valueForLog = $"{rawText.FirstOrDefault()}{new string('*', length)}";
			_log.LogInformation("{TypeName}.{PropertyName} initialized from vault as {ValueForLog}",
				property.DeclaringType?.Name, property.Name, valueForLog);
		} catch (Exception e) {
			_log.LogWarning(e, "Can't log property info");
		}
	}

	private static string ToVaultName(string propertyName) {
		IEnumerable<char> ToChars(string str) {
			bool first = true;
			foreach (var c in str) {
				if (!first && char.IsUpper(c)) {
					yield return '_';
				}
				yield return char.ToLowerInvariant(c);
				first = false;
			}
		}
		return string.Concat(ToChars(propertyName));
	}
}
