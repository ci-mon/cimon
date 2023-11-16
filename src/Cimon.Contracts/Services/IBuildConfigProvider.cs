namespace Cimon.Contracts.Services;

using Cimon.Contracts.CI;

public interface IBuildConfigProvider
{
	Task<IReadOnlyCollection<BuildConfigInfo>> GetAll();
	CISystem CISystem { get; }
}
