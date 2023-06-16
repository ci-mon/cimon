namespace Cimon.Contracts.Services;

public interface IBuildLocatorProvider
{
	IAsyncEnumerable<BuildConfig> GetLocators();
}