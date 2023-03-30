namespace Cimon.Data;

public interface IBuildInfoProvider
{
	public CISystem CiSystem { get; }

	public Task<IList<BuildInfo>> GetInfo(IEnumerable<BuildLocator> locators);
}