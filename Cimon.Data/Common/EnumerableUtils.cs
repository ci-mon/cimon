namespace Cimon.Data;

public static class EnumerableUtils
{
	public static async IAsyncEnumerable<T> SelectMany<TSource, T>(this IEnumerable<TSource> sources, Func<TSource, IAsyncEnumerable<T>> func) {
		foreach (TSource item in sources) {
			await foreach (T innerItem in func(item)) {
				yield return innerItem;
			}
		}
	}

	public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source) {
		var res = new List<T>();
		await foreach (var item in source) {
			res.Add(item);
		}
		return res;
	}
}
