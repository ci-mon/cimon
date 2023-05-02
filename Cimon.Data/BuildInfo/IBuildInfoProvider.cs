namespace Cimon.Data;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }

	Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildLocator> locators);
}