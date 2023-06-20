using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;
using TeamCityAPI;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;

namespace Cimon.Data.TeamCity;

public record struct TeamCityClientTicket(TeamCityClient Client, TcClient Source) : IDisposable
{
	void IDisposable.Dispose() => Source.Return(Client);
	private TcClient Source { get; } = Source;
}

public class TcClient : IPooledObjectPolicy<TeamCityClient>
{
	private readonly TeamCitySecrets _secrets;
	private readonly ObjectPool<TeamCityClient> _clients;
	public TcClient(IOptions<TeamCitySecrets> secrets) {
		_secrets = secrets.Value;
		_clients = new DefaultObjectPool<TeamCityClient>(this, 10);
	}

	public TeamCityClientTicket GetClient() => new(_clients.Get(), this);
	
	public void Return(TeamCityClient client) => _clients.Return(client);

	public async IAsyncEnumerable<BuildConfig> GetBuildConfigs() {
		using var client = GetClient();
		var configs = client.Client.BuildTypes.Include(x => x.BuildType)
			.GetAsyncEnumerable<BuildTypes, BuildType>()
			.Select(buildType => new BuildConfig(buildType.Id, buildType.ProjectName, buildType.WebUrl));
		await foreach (var config in configs) {
			yield return config;
		}
	}

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
}

public record BuildConfig(string Id, string ProjectName, string WebUrl);