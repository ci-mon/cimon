namespace Cimon.DB.Models;

public class Team: IEntityCreator<Team>
{
	public int Id { get; set; }
	public required string Name { get; set; }
	public List<User> Users { get; set; } = new();
	public static Team Create() =>
		new() {
			Name = "team"
		};
}
