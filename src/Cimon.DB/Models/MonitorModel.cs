namespace Cimon.DB.Models;

public record ViewSettings
{
	public List<int> BuildPositions { get; set; } = new();
	public int ColumnsCount { get; set; }
}

public enum MonitorType
{
	Simple,
	Group
}

public record MonitorModel
{
	public int Id { get; set; }
	public required string Key { get; set; }
	public string? Title { get; set; }
	public bool Removed { get; set; }
	public bool AlwaysOnMonitoring { get; set; }
	public bool Shared { get; set; }
	public MonitorType Type { get; set; }
	public User? Owner { get; set; }
	public List<BuildInMonitor> Builds { get; set; } = new();
	public List<ConnectedMonitor> ConnectedMonitors { get; set; } = new();
	public ViewSettings? ViewSettings { get; set; } = new();
}

public class ConnectedMonitor
{
	public MonitorModel SourceMonitorModel { get; set; }
	public int SourceMonitorModelId { get; set; }
	public MonitorModel ConnectedMonitorModel { get; set; }
	public int ConnectedMonitorModelId { get; set; }
}