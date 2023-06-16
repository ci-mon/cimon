using Optional;

namespace Cimon.Data.Common;

public static class OptionalUtils
{
	public static Task MatchSomeAsync<T>(this Option<T> option, Func<T, Task> func) {
		return option.Match(func, () => Task.CompletedTask);
	}
}
