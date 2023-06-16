using Cimon.Contracts;

namespace Cimon.Data.BuildInformation;

public class Monitor
{
	public required string Id { get; set; }
	public List<BuildLocator> Builds { get; set; } = new();
	public string? Title { get; set; }
	public bool Removed { get; set; }
	public bool AlwaysOnMonitoring { get; set; }
}
