namespace Cimon.Data;

public record BuildLocatorDescriptor : BuildLocator
{
	public string Path { get; set; }
}