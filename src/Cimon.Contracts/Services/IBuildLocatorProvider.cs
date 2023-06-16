namespace Cimon.Contracts.Services;

public interface IBuildLocatorProvider
{
	IAsyncEnumerable<BuildLocatorDescriptor> GetLocators();
}