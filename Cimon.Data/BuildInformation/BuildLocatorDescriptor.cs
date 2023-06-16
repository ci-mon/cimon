using Cimon.Contracts;

namespace Cimon.Data.BuildInformation;

public record BuildLocatorDescriptor : BuildLocator
{
	public string? Path { get; set; }
}
