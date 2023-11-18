namespace Cimon.Data.Common;

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

	public static CollectionCompareResult<T> CompareWith<T, TKey>(this IReadOnlyCollection<T>? old, 
			IReadOnlyCollection<T>? other, Func<T, TKey> keySelector) {
		var oldWithHash = (old ?? ArraySegment<T>.Empty).Select(x => new { Item = x, Key = keySelector(x) }).ToList();
		var newWithHash = (other ?? ArraySegment<T>.Empty).Select(x => new { Item = x, Key = keySelector(x) }).ToList();
		var oldHashes = oldWithHash.Select(x => x.Key).ToHashSet();
		var newHashes = newWithHash.Select(x => x.Key).ToHashSet();
		var newItems = new List<T>();
		var removedItems = new List<T>();
		var sameItems = new List<T>();
		foreach (var item in oldWithHash) {
			var dest = !newHashes.Contains(item.Key)
				? removedItems
				: null;
			dest?.Add(item.Item);
		}
		foreach (var item in newWithHash) {
			var dest = oldHashes.Contains(item.Key)
				? sameItems
				: newItems;
			dest.Add(item.Item);
		}
		var result = new CollectionCompareResult<T> {
			Added = newItems,
			Same = sameItems,
			Removed = removedItems
		};
		return result;
	}
}

public struct CollectionCompareResult<T>
{
	public IReadOnlyCollection<T> Same { get; set; }
	public IReadOnlyCollection<T> Removed { get; set; }
	public IReadOnlyCollection<T> Added { get; set; }
}
