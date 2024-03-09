namespace Cimon.Data.ML;

public static class CollectionUtils
{

	public record ItemInfo<T>(T Value, int Index);

	public static ItemInfo<T> GetItemWithMaxValue<T>(this IReadOnlyCollection<T> items) where T : struct, IComparable<T> {
		var value = default(T);
		int index = 0;
		int counter = -1;
		foreach (T item in items) {
			counter++;
			if (item.CompareTo(value) == 1) {
				value = item;
				index = counter;
			}
		}
		return new ItemInfo<T>(value, index);
	}
}
