namespace Cimon.DB.Models;

public record ViewSettings
{
	public List<int> BuildPositions { get; set; } = new();
	public int ColumnsCount { get; set; }
}

public record MonitorModel
{
	public int Id { get; set; }
	public required string Key { get; set; }
	public string? Title { get; set; }
	public bool Removed { get; set; }
	public bool AlwaysOnMonitoring { get; set; }
	public bool Shared { get; set; }
	public User? Owner { get; set; }
	public List<BuildInMonitor> Builds { get; set; } = new();
	public ViewSettings ViewSettings { get; set; } = new();
}
