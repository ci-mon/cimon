namespace Cimon.Data;

public interface IBuildLocatorProvider
{
	IAsyncEnumerable<BuildLocatorDescriptor> GetLocators();
}