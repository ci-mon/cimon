namespace Cimon.Data.Common;

public static class TextUtils
{
	public static string Strip(this string source, int len) =>
		source.Length < len ? source : source[..len];
}
