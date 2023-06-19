using Cimon.Contracts;
using Cimon.Contracts.Services;

namespace Cimon.Data.TeamCity;

public class TcBuildConfigProvider : IBuildConfigProvider
{

	private readonly TcClient _client;
	public TcBuildConfigProvider(TcClient client) {
		_client = client;
	}

	public async Task<IReadOnlyCollection<BuildConfigInfo>> GetAll() {
		var results = new List<BuildConfigInfo>();
		await foreach (var buildConfig in _client.GetBuildConfigs()) {
			var item = new BuildConfigInfo(buildConfig.Id) {
				Props = new Dictionary<string, string> {
					{"ProjectName", buildConfig.ProjectName}
				}
			};
			results.Add(item);
		}
		return results;
	}

	public CISystem CISystem => CISystem.TeamCity;
}
