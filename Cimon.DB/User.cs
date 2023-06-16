namespace Cimon.DB;

public class User : IEntityCreator<User>
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public required string FullName { get; set; }
	public string? Email { get; set; }
	public bool IsDeactivated { get; set; }
	public bool AllowLocalLogin { get; set; }
	public List<Team> Teams { get; set; } = new();
	public List<Role> Roles { get; set; } = new();
	public static User Create() =>
		new() {
			Name = "userName",
			FullName = "User Name"
		};
}
