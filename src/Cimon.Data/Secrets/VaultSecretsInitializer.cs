using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;

namespace Cimon.Data.Secrets;

using System.Configuration;
using System.Reflection;

public class VaultSecretsInitializer<TSecrets> :  IConfigureNamedOptions<TSecrets> where TSecrets : class
{
	private readonly string _prefix = typeof(TSecrets).Name.Replace("Secrets", string.Empty);
	private readonly VaultSecrets _vaultSecrets;
	private readonly ILogger _log;

	public void Configure(TSecrets options) => Configure(null, options);

	public void Configure(string? key, TSecrets options) {
		if (_vaultSecrets.Disabled) return;
		try {
			ConfigureAsync(options, key).ConfigureAwait(false).GetAwaiter().GetResult();
		} catch (Exception e) {
			_log.LogError(e, "Failed to init secrets {SecretType}", typeof(TSecrets).Name);
		}
	}

	public VaultSecretsInitializer(IOptions<VaultSecrets> vaultSettings,
			ILogger<VaultSecretsInitializer<TSecrets>> log) {
		_log = log;
		_vaultSecrets = vaultSettings.Value;
	}

	public TSecrets Get(string key) {
		var secrets = Activator.CreateInstance<TSecrets>();
		Configure(key, secrets);
		return secrets;
	}

	private async Task ConfigureAsync(TSecrets options, string? key) {
		IAuthMethodInfo authMethod = new TokenAuthMethodInfo(_vaultSecrets.Token);
		var vaultClientSettings = new VaultClientSettings(_vaultSecrets.Url, authMethod) {
			VaultServiceTimeout = TimeSpan.FromSeconds(30)
		};
		IVaultClient vaultClient = new VaultClient(vaultClientSettings);
		var secrets = await vaultClient.V1.Secrets.KeyValue.V2
			.ReadSecretAsync(path: _vaultSecrets.Path, mountPoint: _vaultSecrets.MountPoint).ConfigureAwait(false);
		var prefix = string.IsNullOrWhiteSpace(key) ? _prefix : $"{_prefix}.{key}";
		var config = new Microsoft.Extensions.Configuration.ConfigurationManager();
		if (secrets.Data.Data.TryGetValue(prefix, out var data) && data is JsonElement jsonVal) {
			var jsonStream = new MemoryStream();
			await using (var writer = new Utf8JsonWriter(jsonStream)) {
				jsonVal.WriteTo(writer);
			}
			jsonStream.Seek(0, SeekOrigin.Begin);
			config.AddJsonStream(jsonStream);
		} else {
			var values = new Dictionary<string, string?>();
			foreach (var item in secrets.Data.Data) {
				var dataKey = item.Key;
				if (!dataKey.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
				values[dataKey.Substring(prefix.Length + 1)] = item.Value?.ToString();
			}
			if (values.Count == 0) {
				return;
			}
			config.AddInMemoryCollection(values);
		}
		config.Bind(options);
		_log.LogInformation("{TypeName} initialized from vault: {@Config}", typeof(TSecrets).Name, options);
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

}
