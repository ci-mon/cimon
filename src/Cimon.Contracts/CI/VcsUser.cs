namespace Cimon.Contracts.CI;

public record VcsUser(UserName Name, string FullName, string? Email = null);