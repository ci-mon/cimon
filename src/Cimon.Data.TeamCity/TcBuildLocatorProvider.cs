using Cimon.Contracts;
using Cimon.Contracts.Services;

namespace Cimon.Data.TeamCity;

public class TcBuildConfigProvider : IBuildConfigProvider
{

	private readonly TcClient _client;
	public TcBuildConfigProvider(TcClient client) {
		_client = client;
	}

#pragma warning disable CS1998
	public async IAsyncEnumerable<BuildConfigInfo> GetAll() {
#pragma warning restore CS1998
		var buildConfigs = _client.GetBuildConfigs().All();
		foreach (var buildConfig in buildConfigs) {
			if (buildConfig.Personal is true || buildConfig.Cancelled is true) {
				continue;
			}
			yield return new BuildConfigInfo {
				Key = buildConfig.Id,
				Props = new Dictionary<string, string> {
					{"Path", buildConfig.ProjectName}
				}
			};
		}
	}

	public CISystem CISystem => CISystem.TeamCity;
}
