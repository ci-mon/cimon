namespace Cimon.Data.TeamCity;

public class TcBuildInfoProvider : IBuildInfoProvider
{

	public CISystem CiSystem => CISystem.TeamCity;

	public async Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildLocator> locators) {
		return MockData.TestBuildInfos.Where(b => locators.Any(l=>l.Id == b.Name)).ToList();
	}
}
