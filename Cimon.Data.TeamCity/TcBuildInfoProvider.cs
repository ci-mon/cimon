namespace Cimon.Data.TeamCity;

using System.Text.Json;

public class TcBuildInfoProvider:IBuildInfoProvider
{

	public CISystem CiSystem => CISystem.TeamCity;

	public async Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildLocator> locators) {
		/*var buildInfos = locators.Select(l => new BuildInfo {
			BuildId = l.Id,
			Name = $"build {l.Id} {++counter}"
		}).ToList();*/
		return MockData.TestBuildInfos.Where(b => locators.Any(l=>l.Id == b.Name)).ToList();
	}
}
