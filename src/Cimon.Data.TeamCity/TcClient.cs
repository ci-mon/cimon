using Microsoft.Extensions.Options;
using TeamCityAPI;
using TeamCityAPI.Locators;
using TeamCityAPI.Locators.Common;
using TeamCityAPI.Models;
using TeamCityAPI.Queries;

namespace Cimon.Data.TeamCity;

public class TcClient
{
	private readonly TeamCitySecrets _secrets;
	public TcClient(IOptions<TeamCitySecrets> secrets) {
		_secrets = secrets.Value;
	}

	public TeamCityClient CreateClient() {
		var client = new TeamCityClient(_secrets.Uri.ToString());
		if (_secrets.Token?.Length > 0) {
			client.UseToken(_secrets.Token);
		}
		else if (_secrets.Login?.Length > 0) {
			client.UseLoginAndPass(_secrets.Login, _secrets.Password);
		}
		return client;
	}

	public IAsyncEnumerable<BuildConfig> GetBuildConfigs() {
		var client = CreateClient();
		return client.BuildTypes.Include(x => x.BuildType)
			.GetAsyncEnumerable<BuildTypes, BuildType>()
			.Select(buildType => new BuildConfig(buildType.Id, buildType.ProjectName, buildType.WebUrl));
	}
}

public record BuildConfig(string Id, string ProjectName, string WebUrl);