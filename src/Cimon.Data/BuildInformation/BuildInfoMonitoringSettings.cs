namespace Cimon.Data.BuildInformation;

public class BuildInfoMonitoringSettings
{
	public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(5);

	public string[] SystemUserLogins { get; set; } = Array.Empty<string>();
}
