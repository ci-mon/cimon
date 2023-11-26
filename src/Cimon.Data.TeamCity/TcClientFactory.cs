using System.Reflection;
using Cimon.Contracts.Services;
using Microsoft.Extensions.Options;
using TeamCityAPI;

namespace Cimon.Data.TeamCity;

public record struct TeamCityClientTicket(TeamCityClient Client, TeamcitySecrets Secrets) : IDisposable
{
	void IDisposable.Dispose() {
		var type = typeof(TeamCityClient);
		var httpClient = (HttpClient)type.GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)
			?.GetValue(Client)!;
		httpClient.Dispose();
	}
}

public class TcClientFactory(IOptionsSnapshot<TeamcitySecrets> snapshot)
{

	public TeamCityClientTicket Create(string connectorKey) {
		var secrets = snapshot.Get(connectorKey);
		if(secrets.Uri is null) {
			throw new InvalidOperationException($"URL for teamcity {connectorKey} is not defined");
		}
		var client = new TeamCityClient(secrets.Uri.ToString());
		if (secrets.Token?.Length > 0) {
			client.UseToken(secrets.Token);
		}
		else if (secrets.Login?.Length > 0) {
			client.UseLoginAndPass(secrets.Login, secrets.Password);
		}
		return new TeamCityClientTicket(client, secrets);
	}


}
