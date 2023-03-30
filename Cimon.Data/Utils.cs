namespace Cimon.Data;

using System.Reactive.Linq;

public static class ObservableUtils
{
	public static IObservable<TSource> OnSubscribe<TSource>(this IObservable<TSource> source, Action onSubscribe,
		Action onDispose) {
		return Observable.Create<TSource>(observer => {
			onSubscribe?.Invoke();
			IDisposable subscription = source.Subscribe(observer);
			return () => {
				subscription.Dispose();
				onDispose?.Invoke();
			};
		});
	}
}
