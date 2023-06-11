namespace Cimon.DB;

public class User
{
	public int Id { get; set; }
	public string Name { get; set; }
	public string FullName { get; set; }

	public bool IsDeactivated { get; set; }
	public List<Team> Teams { get; set; } = new();
	public List<Role> Roles { get; set; } = new();
}
