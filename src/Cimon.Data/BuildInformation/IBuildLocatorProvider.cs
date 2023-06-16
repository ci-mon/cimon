namespace Cimon.Data.BuildInformation;

public interface IBuildLocatorProvider
{
	IAsyncEnumerable<BuildLocatorDescriptor> GetLocators();
}