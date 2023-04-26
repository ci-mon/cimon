namespace Cimon.Auth;

public class PasswordSignInResult
{
	public bool Success { get; set; }
	public UserName UserName { get; set; }
	public string Team { get; set; }
}
