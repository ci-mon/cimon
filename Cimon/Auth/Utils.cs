namespace Cimon.Auth;

using System.Security.Principal;

public record struct UserName(string Domain, string Name)
{
	public override string ToString() => $"{Domain}\\{Name}";

	public static implicit operator string(UserName? name) => name?.ToString() ?? string.Empty;

	public static implicit operator UserName(string? name) {
		if (name == null) {
			return new UserName(string.Empty, string.Empty);
		}
		int indexOfDomain = name.IndexOf(@"\", StringComparison.Ordinal);
		return indexOfDomain > 0
			? new UserName(name[..indexOfDomain], name[(indexOfDomain + 1)..])
			: new UserName(string.Empty, name);
	}
}

public static class Utils
{
	public static string GetUserName(this IIdentity? source) {
		return ((UserName)source?.Name).Name;
	}
}
