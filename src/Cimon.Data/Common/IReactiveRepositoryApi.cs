namespace Cimon.Data.Common;

public interface IReactiveRepositoryApi<T>
{
	Task<T> LoadData(CancellationToken token);
}
