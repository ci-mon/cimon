namespace Cimon.Contracts.Services;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }

	Task<IReadOnlyCollection<BuildInfo>> GetInfo(IReadOnlyList<BuildInfoQuery> infoQueries);
}
