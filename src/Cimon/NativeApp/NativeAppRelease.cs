using System.Collections.Immutable;

namespace Cimon.NativeApp;

public enum NativeAppPlatform { Windows, Mac, Linux }

public record NativeAppReleaseArtifact(NativeAppPlatform Platform, string FileName);

public record NativeAppRelease(Version Version, string ReleaseNotes, IImmutableList<NativeAppReleaseArtifact> Artifacts);

