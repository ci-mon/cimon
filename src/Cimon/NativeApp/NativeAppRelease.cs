using System.Collections.Immutable;

namespace Cimon.NativeApp;

public enum NativeAppPlatform { Linux, Win32, Darwin, Mas }
public enum NativeAppArchitecture { Ia32 , X64 , Armv7L , Arm64 , Mips64El , Universal }

public record NativeAppReleaseArtifact(NativeAppRelease Release, NativeAppPlatform Platform, NativeAppArchitecture Architecture, string FileName, DateTime CreatedOn)
{
	public long FileLength { get; set; }
	public string Sha1 { get; set; }
	public string Extension => Path.GetExtension(FileName);
	private static readonly HashSet<string> UpdateOnlyExtensions = new(StringComparer.OrdinalIgnoreCase) {
		".nupkg"
	};
	public bool IsForUpdateOnly => UpdateOnlyExtensions.Contains(Extension);
}

public record NativeAppRelease(Version Version, string ReleaseNotes)
{
	public IImmutableList<NativeAppReleaseArtifact> Artifacts { get; set; }
}
