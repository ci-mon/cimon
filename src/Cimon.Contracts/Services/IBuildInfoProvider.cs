namespace Cimon.Contracts.Services;

using CI;

public record LogsQuery(CIConnectorInfo ConnectorInfo, BuildConfig BuildConfig, BuildInfo BuildInfo, CancellationToken CancellationToken);
public interface IBuildInfoProvider
{
	Task<IReadOnlyList<BuildInfo>> FindInfo(BuildInfoQuery infoQuery);
	Task<string> GetLogs(LogsQuery logsQuery);
}
