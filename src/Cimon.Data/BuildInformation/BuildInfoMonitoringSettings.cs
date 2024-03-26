namespace Cimon.Data.BuildInformation;

public class BuildInfoMonitoringSettings
{
	public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(15);

	public string[] SystemUserLogins { get; set; } = Array.Empty<string>();
}
