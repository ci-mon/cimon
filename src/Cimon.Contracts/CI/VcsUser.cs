namespace Cimon.Contracts.CI;

public record VcsUser(UserName Name, string FullName, string? Email = null)
{
	public string SafeName => string.IsNullOrWhiteSpace(Name) ? "<unknown>" : Name;
	public string SafeFullName => string.IsNullOrWhiteSpace(FullName) ? SafeName : FullName;
}
