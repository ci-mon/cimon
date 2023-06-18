using System.Collections.Immutable;

namespace Cimon.Contracts.Services;

public interface IBuildConfigProvider
{
	Task<IReadOnlyCollection<BuildConfigInfo>> GetAll();
	CISystem CISystem { get; }
}
