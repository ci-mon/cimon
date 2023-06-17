namespace Cimon.Data.Secrets;

public class VaultSettings
{
	public bool Disabled { get; set; }
	public required string Url { get; set; }
	public required string Token { get; set; }
	public required string MountPoint { get; set; }
	public required string Path { get; set; }
}
