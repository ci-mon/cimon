namespace Cimon.Contracts.Services;

using CI;

public interface IBuildInfoProvider
{
	Task<BuildInfo?> FindInfo(BuildInfoQuery infoQuery);
}
