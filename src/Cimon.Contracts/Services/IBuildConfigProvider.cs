namespace Cimon.Contracts.Services;

using CI;

public record CIConnectorInfo(string ConnectorKey, IReadOnlyDictionary<string, string> Settings);
public interface IBuildConfigProvider
{
	Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info);
	Dictionary<string, string> GetSettings();
}
