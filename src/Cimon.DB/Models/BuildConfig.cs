using Cimon.Contracts;

namespace Cimon.DB.Models;

public class BuildConfig : BuildConfigInfo
{
	public int Id { get; set; }
	public CISystem CISystem { get; init; }
	public BuildInfo? DemoState { get; set; }
	public List<Monitor> Monitors { get; set; } = new();
	public override bool Equals(object? obj) => obj is BuildConfig buildConfig && Equals(buildConfig);
	protected bool Equals(BuildConfig other) => base.Equals(other) && CISystem == other.CISystem;
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), (int)CISystem);
}
