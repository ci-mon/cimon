using System.Collections.Immutable;

namespace Cimon.Data.BuildInformation;

public interface IBuildMonitoringService
{
	Task CheckBuildInfo(IImmutableList<Contracts.BuildInfo> buildInfos);
}
