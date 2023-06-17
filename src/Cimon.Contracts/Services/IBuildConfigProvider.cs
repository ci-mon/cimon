namespace Cimon.Contracts.Services;

public interface IBuildConfigProvider
{
	IAsyncEnumerable<BuildConfigInfo> GetAll();
	CISystem CISystem { get; }
}
