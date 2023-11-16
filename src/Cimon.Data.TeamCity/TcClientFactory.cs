using System.Reflection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using TeamCityAPI;

namespace Cimon.Data.TeamCity;

public record struct TeamCityClientTicket(TeamCityClient Client, TcClientFactory Source) : IDisposable
{
	void IDisposable.Dispose() => Source.Return(Client);
	private TcClientFactory Source { get; } = Source;
}

public class TcClientFactory : IPooledObjectPolicy<TeamCityClient>
{
	private readonly TeamcitySecrets _secrets;
	private readonly ObjectPool<TeamCityClient> _clients;
	public TcClientFactory(IOptions<TeamcitySecrets> secrets) {
		_secrets = secrets.Value;
		_clients = new DefaultObjectPool<TeamCityClient>(this, 10);
	}

	public TeamCityClientTicket GetClient() => new(_clients.Get(), this);
	
	public void Return(TeamCityClient client) => _clients.Return(client);

	public TeamCityClient Create() {
		var client = new TeamCityClient(_secrets.Uri.ToString());
		if (_secrets.Token?.Length > 0) {
			client.UseToken(_secrets.Token);
		}
		else if (_secrets.Login?.Length > 0) {
			client.UseLoginAndPass(_secrets.Login, _secrets.Password);
		}
		return client;
	}

	bool IPooledObjectPolicy<TeamCityClient>.Return(TeamCityClient obj) => true;

	public async Task<string> GetLogsAsync(long buildId) {
		using var clientTicket = GetClient();
		var client = clientTicket.Client;
		var type = typeof(TeamCityClient);
		var httpClient = (HttpClient)type.GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(client)!;
		var address = _secrets.Uri + $"/httpAuth/downloadBuildLog.html?buildId={buildId}";
		return await httpClient.GetStringAsync(address);
	}
}

public record BuildConfig(string Id, string ProjectName, string WebUrl);