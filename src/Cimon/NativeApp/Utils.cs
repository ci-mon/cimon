namespace Cimon.NativeApp;

public static class Utils
{
	public static string ToShortString(this Version version) => $"{version.Major}.{version.Minor}.{version.Build}";
}