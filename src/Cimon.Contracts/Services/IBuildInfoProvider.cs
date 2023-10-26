namespace Cimon.Contracts.Services;

using Cimon.Contracts.CI;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }

	Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildInfoQuery> infoQueries);
}
