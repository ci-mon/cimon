namespace Cimon.Data.Jenkins;

public class JenkinsSecrets
{
	public required Uri Uri { get; set; }
	public string? Login { get; set; }
	public string? Token { get; set; }
}