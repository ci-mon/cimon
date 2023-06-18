using Cimon.Contracts;
using Cimon.Contracts.Services;

namespace Cimon.Data.TeamCity;

public class TcBuildConfigProvider : IBuildConfigProvider
{

	private readonly TcClient _client;
	public TcBuildConfigProvider(TcClient client) {
		_client = client;
	}

	public Task<IReadOnlyCollection<BuildConfigInfo>> GetAll() {
		var buildConfigs = _client.GetBuildConfigs().All();
		var results = new List<BuildConfigInfo>();
		foreach (var buildConfig in buildConfigs) {
			if (buildConfig.Personal is true || buildConfig.Cancelled is true) {
				continue;
			}
			var item = new BuildConfigInfo(buildConfig.Id) {
				Props = new Dictionary<string, string> {
					{"ProjectName", buildConfig.ProjectName}
				}
			};
			results.Add(item);
		}
		return Task.FromResult((IReadOnlyCollection<BuildConfigInfo>)results);
	}

	public CISystem CISystem => CISystem.TeamCity;
}
