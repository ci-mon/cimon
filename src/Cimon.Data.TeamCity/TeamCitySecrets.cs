namespace Cimon.Data.TeamCity;

public class TeamCitySecrets
{
	public required Uri Uri { get; set; }
	public string? Login { get; set; }
	public string? Password { get; set; }
	public string? Token { get; set; }
}