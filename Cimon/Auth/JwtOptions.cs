namespace Cimon.Auth;

public class JwtOptions
{
	public string Issuer { get; set; } = "cimon";
	public string Audience { get; set; } = "cimon-electron";
	public byte[] Key { get; set; } = "!SomethingSecret!"u8.ToArray();
	public TimeSpan Expiration { get; set; } = TimeSpan.FromHours(8);
}
