namespace Cimon.Contracts;

public record BuildLocatorDescriptor : BuildLocator
{
	public string? Path { get; set; }
}
