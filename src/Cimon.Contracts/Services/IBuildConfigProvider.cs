namespace Cimon.Contracts.Services;

using CI;

public record CIConnectorInfo(IReadOnlyDictionary<string, string> Settings);
public interface IBuildConfigProvider
{
	Task<IReadOnlyCollection<BuildConfig>> GetAll(CIConnectorInfo info);
	CISystem CISystem { get; }
	Dictionary<string, string> GetSettings();
}
