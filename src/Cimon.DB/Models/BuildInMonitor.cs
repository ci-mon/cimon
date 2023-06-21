namespace Cimon.DB.Models;

public class BuildInMonitor
{
	public Monitor Monitor { get; set; }
	public int MonitorId { get; set; }
	public BuildConfig BuildConfig { get; set; }
	public int BuildConfigId { get; set; }
}
