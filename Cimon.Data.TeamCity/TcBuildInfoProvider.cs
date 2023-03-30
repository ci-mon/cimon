namespace Cimon.Data.TeamCity;

public class TcBuildInfoProvider:IBuildInfoProvider
{
	private int counter;

	public CISystem CiSystem => CISystem.TeamCity;

	public Task<IList<BuildInfo>> GetInfo(IEnumerable<BuildLocator> locators) {
		return Task.FromResult((IList<BuildInfo>)locators.Select(l => new BuildInfo {
			BuildId = l.Id,
			Name = $"build {l.Id} {++counter}"
		}).ToList());
	}
}
