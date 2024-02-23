namespace Cimon.Shared;

public class DebugMode
{
	static DebugMode() {
#if DEBUG
		Enabled = true;
#endif
	}

	public static bool Enabled { get; set; }
}
