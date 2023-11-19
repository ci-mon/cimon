namespace Cimon.DB.Models;

using Cimon.Contracts.CI;

public enum BuildConfigStatus
{
	Ok,
	NotFoundInCISystem
}

public record BuildConfig(CISystem CISystem, string Key, string? Branch = null, bool IsDefaultBranch = false) 
	: BuildConfigInfo(Key, Branch, IsDefaultBranch)
{
	public BuildConfigStatus Status { get; set; }
	public BuildInfo? DemoState { get; set; }
	public List<BuildInMonitor> Monitors { get; set; } = new();
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), (int)CISystem);
	public virtual bool Equals(BuildConfig? other) => (other?.CISystem.Equals(CISystem) ?? false) && base.Equals(other);
}
