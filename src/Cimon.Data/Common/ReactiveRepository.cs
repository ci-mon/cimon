using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Cimon.Data.Common;

public class ReactiveRepository<T>
{
	private readonly IReactiveRepositoryApi<T> _api;
	private readonly ReplaySubject<T> _bufferedItems;

	public ReactiveRepository(IReactiveRepositoryApi<T> api) {
		_api = api;
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
		Items = _bufferedItems.Merge(loadDataOnce);
	}

	public IObservable<T> Items { get; }

	public async Task Mutate(Func<T, Task<T>> func) {
		var item = await Items.FirstAsync();
		var newItem = await func(item);
		_bufferedItems.OnNext(newItem);
	}

	public async Task Refresh(bool silent = false) {
		var data = await _api.LoadData(default);
		if (!silent) {
			_bufferedItems.OnNext(data);
		}
	}
}
