using Cimon.Contracts.Services;

namespace Cimon.Contracts.CI;

public record BuildConfig
{
	public Dictionary<string, string> Props { get; set; }
	public int Id { get; init; }
	public string Key { get; init; }
	public string? Branch { get; init; }
	public string? Name { get; init; }
	public bool IsDefaultBranch { get; init; }
	public bool IsSame(BuildConfig? other) {
		return Key == other?.Key && Branch == other.Branch;
	}
}

public record BuildInfoQueryOptions(string? LastBuildId = null);
public record BuildInfoQuery(CIConnectorInfo ConnectorInfo, BuildConfig BuildConfig,
	BuildInfoQueryOptions? Options = null);
