using Microsoft.Extensions.Options;
using TeamCitySharp;
using TeamCitySharp.ActionTypes;

namespace Cimon.Data.TeamCity;

public class TcClient
{
	private readonly TeamCitySecrets _secrets;
	public TcClient(IOptions<TeamCitySecrets> secrets) {
		_secrets = secrets.Value;
	}

	public IBuildConfigs GetBuildConfigs() {
		var client = CreateClient();
		return client.BuildConfigs;
	}
	public IBuilds GetBuilds() {
		var client = CreateClient();
		return client.Builds;
	}

	public TeamCityClient CreateClient() {
		var client = new TeamCityClient($"{_secrets.Uri.Host}:{_secrets.Uri.Port}",
			_secrets.Uri.Scheme.ToLowerInvariant() != "http");
		if (_secrets.Token?.Length > 0) {
			client.ConnectWithAccessToken(_secrets.Token);
		}
		else if (_secrets.Login?.Length > 0) {
			client.Connect(_secrets.Login, _secrets.Password);
		}
		else {
			client.ConnectAsGuest();
		}
		return client;
	}
}