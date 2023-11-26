namespace Cimon.Contracts.CI;

public record BuildConfig
{
	public Dictionary<string, string> Props { get; set; }
	public int Id { get; init; }
	public string Key { get; init; }
	public string? Branch { get; init; }
	public bool IsDefaultBranch { get; init; }
}

public record BuildInfoQueryOptions(string? LastBuildNumber = null);
public record BuildInfoQuery(BuildConfig BuildConfig, BuildInfoQueryOptions? Options = null);
