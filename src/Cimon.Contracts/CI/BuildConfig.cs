using Cimon.Contracts.Services;

namespace Cimon.Contracts.CI;

public record BuildConfig
{
	public Dictionary<string, string> Props { get; set; } = new();
	public int Id { get; init; }
	public string Key { get; init; } = null!;
	public string? Branch { get; init; }
	public string? Name { get; init; }
	public bool IsDefaultBranch { get; init; }
	public bool AllowML { get; set; } = true;
	public bool IsSame(BuildConfig? other) {
		return Key == other?.Key && Branch == other.Branch;
	}
}

public record BuildInfoQueryOptions(string? LastBuildId, int LookBackLimit);

public record BuildInfoQuery(CIConnectorInfo ConnectorInfo, BuildConfig BuildConfig,
	BuildInfoQueryOptions Options);
