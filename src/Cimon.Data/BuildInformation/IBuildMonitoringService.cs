using System.Collections.Immutable;

namespace Cimon.Data.BuildInformation;

public interface IBuildMonitoringService
{
	Task CheckBuildInfo(ImmutableArray<Contracts.BuildInfo> buildInfos);
}