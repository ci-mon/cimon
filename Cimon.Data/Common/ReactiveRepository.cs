using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cimon.Data.Common;

public class ReactiveRepository<T>
{
	private readonly ReplaySubject<T> _bufferedItems;
	private readonly IObservable<T> _allItems;

	public ReactiveRepository(IReactiveRepositoryApi<T> api) {
		_bufferedItems = new ReplaySubject<T>(1);
		var loadDataOnce = Observable
			.DeferAsync(async ct => {
				var data = await api.LoadData(ct);
				_bufferedItems.OnNext(data);
				return Observable.Empty<T>();
			})
			.Take(1)
			.Publish()
			.RefCount();
		_allItems = _bufferedItems.Merge(loadDataOnce);
	}

	public IObservable<T> Items => _allItems;

	public async Task Mutate(Func<T, Task<T>> func) {
		var item = await Items.FirstAsync();
		var newItem = await func(item);
		_bufferedItems.OnNext(newItem);
	}

}