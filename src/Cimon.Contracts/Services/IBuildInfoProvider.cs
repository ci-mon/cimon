namespace Cimon.Contracts.Services;

using CI;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }

	Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildInfoQuery> infoQueries);
	Task<BuildInfo?> FindInfo(BuildInfoQuery infoQuery);
}
