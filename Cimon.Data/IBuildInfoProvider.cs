namespace Cimon.Data;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }

	Task<IList<BuildInfo>> GetInfo(IEnumerable<BuildLocator> locators);
}