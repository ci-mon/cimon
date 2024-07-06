using Cimon.Data.BuildInformation;

namespace Cimon.Data.Secrets;

public class CimonSecrets
{
	public JwtOptions Jwt { get; set; } = new();
	public AuthOptions Auth { get; set; } = new();
	public BuildInfoMonitoringSettings BuildInfoMonitoring { get; set; } = new();
}
