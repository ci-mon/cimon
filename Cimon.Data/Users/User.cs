namespace Cimon.Data.Users;

public record User(UserId Id, string Name, string? Team = null)
{
	public static User Guest { get; } = new User("guest", "Guest");

	public static User FromFullName(string fullName) {
		var id = fullName.Split(" ").Aggregate((a, b) => $"{Char.ToLowerInvariant(a[0])}.{b.ToLowerInvariant()}");
		return new User(id, fullName, String.Empty);
	}
	public string Email => $"{Id}@creatio.com";
}
