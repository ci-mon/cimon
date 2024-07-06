using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.AuthMethods.UserPass;

namespace Cimon.Data.Secrets;

public class VaultConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
	private readonly string _prefix;
	private readonly VaultSecrets _vaultSettings;
	private readonly IHostEnvironment _environment;
	private readonly ILogger<VaultConfigurationProvider> _log;

	public VaultConfigurationProvider(string prefix, VaultSecrets vaultSettings, IHostEnvironment environment,
			ILogger<VaultConfigurationProvider> log) {
		_prefix = prefix;
		_vaultSettings = vaultSettings;
		_environment = environment;
		_log = log;
	}

	internal IVaultClient CreateVaultClient(TimeSpan timeout) {
		_log.LogDebug("Using vault credentials: {@VaultCredentials}", _vaultSettings);
		IAuthMethodInfo authMethod = string.IsNullOrWhiteSpace(_vaultSettings.UserName)
			? new TokenAuthMethodInfo(_vaultSettings.Token)
			: new UserPassAuthMethodInfo(_vaultSettings.UserName, _vaultSettings.Password);
		var vaultClientSettings = new VaultClientSettings(_vaultSettings.Url, authMethod) {
			VaultServiceTimeout = timeout
		};
		IVaultClient vaultClient = new VaultClient(vaultClientSettings);
		return vaultClient;
	}

	private async Task<JsonObject> ReadDataInVault() {
		var vaultClient = CreateVaultClient(TimeSpan.FromSeconds(30));
		var path = _vaultSettings.Path is {Length:>0} setPath ? setPath : _environment.EnvironmentName;
		var secrets = await vaultClient.V1.Secrets.KeyValue.V2
			.ReadSecretAsync(path: path, mountPoint: _vaultSettings.MountPoint)
			.ConfigureAwait(false);
		var valuePairs = secrets.Data.Data.Select(x =>
			new KeyValuePair<string, JsonNode?>($"{_prefix}:{x.Key}", x.Value switch {
				string s => JsonValue.Create(s),
				JsonElement e => e.Deserialize<JsonNode>(),
				_ => null
			}));
		return new JsonObject(valuePairs!);
	}

	class JsonObjectConfigurationProvider()
		: JsonStreamConfigurationProvider(new JsonStreamConfigurationSource())
	{
		public IDictionary<string, string?> Load(JsonObject data) {
			using var stream = new MemoryStream();
			using (var writer = new Utf8JsonWriter(stream)) {
				data.WriteTo(writer);
			}
			stream.Seek(0, SeekOrigin.Begin);
			Source.Stream = stream;
			Load();
			return Data;
		}
	}

	/// <inheritdoc />
	public override void Load() {
		if (_vaultSettings.Disabled) return;
		if (string.IsNullOrWhiteSpace(_vaultSettings.Token) && string.IsNullOrWhiteSpace(_vaultSettings.UserName)) return;
		try {
			var data = ReadDataInVault().GetAwaiter().GetResult();
			var loader = new JsonObjectConfigurationProvider();
			Data = loader.Load(data);
			_log.LogInformation("{Count} keys loaded from vault using {@VaultSettings}", Data.Count, _vaultSettings);
		} catch (Exception e) {
			_log.LogError(e, "Failed to init secrets from vault. Config {@VaultSettings}", _vaultSettings);
		}
	}

	public IConfigurationProvider Build(IConfigurationBuilder builder) => this;
	public int LoadedKeysCount => Data.Count;
}
