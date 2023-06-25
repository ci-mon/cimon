namespace Cimon.Contracts;

public record BaseBuildConfigInfo(string Key, string? Branch, bool IsDefaultBranch);

public record BuildConfigInfo(string Key, string? Branch = null, bool IsDefaultBranch = false) : BaseBuildConfigInfo(Key, Branch, IsDefaultBranch)
{
	public Dictionary<string, string> Props { get; set; } = new();
	public virtual bool Equals(BuildConfigInfo? other) => base.Equals(other);
	public override int GetHashCode() => base.GetHashCode();
}

public record BuildInfoQueryOptions(string? LastBuildNumber = null);
public record BuildInfoQuery(BuildConfigInfo BuildConfig, BuildInfoQueryOptions? Options = null);