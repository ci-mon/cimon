namespace Cimon.Contracts.Services;

using CI;

public interface IBuildInfoProvider
{
	CISystem CiSystem { get; }
	Task<BuildInfo?> FindInfo(BuildInfoQuery infoQuery);
}
