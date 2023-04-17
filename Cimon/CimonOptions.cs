namespace Cimon;

using Cimon.Auth;
using Cimon.Data;

public class CimonOptions
{
	
	public JwtOptions Jwt { get; set; } = new();
	public AuthOptions Auth { get; set; } = new();
	public BuildInfoMonitoringSettings BuildInfoMonitoring { get; set; } = new();
}
