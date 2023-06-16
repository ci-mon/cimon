namespace Cimon.Contracts;

public record BuildConfig : BuildLocator
{
	public string? Path { get; set; }
}
