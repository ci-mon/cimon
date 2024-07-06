namespace Cimon.Data.Secrets;

public class AuthOptions
{
	public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(8);
}
