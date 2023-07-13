using System.Collections.Immutable;
using Microsoft.Extensions.Options;

namespace Cimon.NativeApp;

public class NativeAppRepositorySettings
{
	public string ArtifactsPath { get; set; } = "./nativeApps";
}

public static class IOUtils
{
	public static string ReadAllText(this FileInfo file) {
		using var reader = file.OpenText();
		return reader.ReadToEnd();
	}
}

public class NativeAppService
{
	private readonly NativeAppRepositorySettings _settings;
	public NativeAppService(IOptions<NativeAppRepositorySettings> options) {
		_settings = options.Value;
	}

	public IReadOnlyCollection<NativeAppRelease> GetReleases() {
		var results = new List<NativeAppRelease>();
		foreach (var directory in Directory.EnumerateDirectories(_settings.ArtifactsPath).Select(x=>new DirectoryInfo(x))) {
			if (!Version.TryParse(directory.Name, out var version)) {
				continue;
			}
			var artifacts = new List<NativeAppReleaseArtifact>();
			foreach (var fileInfo in directory.EnumerateFiles()) {
				if (fileInfo.Extension == ".exe") {
					artifacts.Add(new NativeAppReleaseArtifact(NativeAppPlatform.Windows, fileInfo.Name));
				}
			}
			var notes = directory.GetFiles("readme.md").FirstOrDefault()?.ReadAllText() ?? string.Empty;
			var appRelease = new NativeAppRelease(version, notes, artifacts.ToImmutableList());
			results.Add(appRelease);
		}
		return results;
	}

	public Stream ReadArtifact(Version version, NativeAppPlatform platform, string fileName) {
		var path = Path.Combine(_settings.ArtifactsPath, version.ToShortString(), fileName);
		return File.OpenRead(path);
	}

	public void Refresh() {
		
	}
}