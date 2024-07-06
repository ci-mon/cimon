namespace Cimon.Data.Secrets;

public class VaultSecrets
{
	public bool Disabled { get; set; }
	public required string Url { get; set; }
	public string? Token { get; set; }
	public string? UserName { get; set; }
	public string? Password { get; set; }
	public required string MountPoint { get; set; }
	public string? Path { get; set; }
}
