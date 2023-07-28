namespace Cimon.NativeApp;

public static class IOUtils
{
	public static string ReadAllText(this FileInfo file) {
		using var reader = file.OpenText();
		return reader.ReadToEnd();
	}
}