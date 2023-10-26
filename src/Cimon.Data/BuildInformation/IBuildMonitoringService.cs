using System.Collections.Immutable;

namespace Cimon.Data.BuildInformation;

using Cimon.Contracts.CI;

public interface IBuildMonitoringService
{
	Task CheckBuildInfo(IImmutableList<BuildInfo> buildInfos);
}
