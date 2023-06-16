namespace Cimon.Data.TeamCity;

public class TeamCitySecrets
{
	public required Uri Uri { get; set; }
	public required string Login { get; set; }
	public required string Password { get; set; }
}