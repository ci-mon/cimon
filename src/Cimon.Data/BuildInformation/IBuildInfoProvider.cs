using Cimon.Contracts;

namespace Cimon.Data.BuildInformation;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }

	Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildLocator> locators);
}