using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cimon.Contracts.Services;

using CI;

public record CIConnectorInfo(string ConnectorKey, IReadOnlyDictionary<string, string> Settings);
public interface IBuildConfigProvider
{
	Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info, Action<int>? reportProgress);
	Dictionary<string, string> GetSettings();
	Task<HealthCheckResult> CheckHealth(CIConnectorInfo info);
}
