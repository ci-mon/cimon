namespace Cimon.Data;

public interface IBuildInfoProvider
{
	public Task<IList<BuildInfo>> GetInfo(IList<BuildLocator> locators);
}