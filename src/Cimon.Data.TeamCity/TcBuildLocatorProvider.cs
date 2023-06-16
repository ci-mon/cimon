using Cimon.Contracts;
using Cimon.Contracts.Services;

namespace Cimon.Data.TeamCity;

public class TcBuildLocatorProvider : IBuildLocatorProvider
{

	private readonly TcClient _client;
	public TcBuildLocatorProvider(TcClient client) {
		_client = client;
	}

#pragma warning disable CS1998
	public async IAsyncEnumerable<BuildConfig> GetLocators() {
#pragma warning restore CS1998
		var buildConfigs = _client.GetBuildConfigs().All();
		foreach (var buildConfig in buildConfigs) {
			if (buildConfig.Personal is true || buildConfig.Cancelled is true) {
				continue;
			}
			yield return new BuildConfig {
				Id = buildConfig.Id,
				CiSystem = CISystem.TeamCity,
				Path = buildConfig.ProjectName
			};
		}
	}
}
