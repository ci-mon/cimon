namespace Cimon.Auth;

public class AuthOptions
{
	public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(8);
}
