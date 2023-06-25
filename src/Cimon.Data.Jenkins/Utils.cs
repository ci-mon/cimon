namespace Cimon.Data.Jenkins;

static class Utils
{
	public static DateTimeOffset ToDate(this long timestamp) {
		return DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
	}
}