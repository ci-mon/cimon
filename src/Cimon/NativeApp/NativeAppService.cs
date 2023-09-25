using System.Collections.Immutable;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Cimon.NativeApp;

public class NativeAppService
{
	private readonly NativeAppRepositorySettings _settings;
	private readonly ILogger _logger;
	private readonly IMemoryCache _cache;
	private readonly string _changelogFileName = "CHANGELOG.md";

	public NativeAppService(IOptions<NativeAppRepositorySettings> options, ILogger<NativeAppService> logger, 
			IMemoryCache cache) {
		_logger = logger;
		_cache = cache;
		_settings = options.Value;
	}

	public IReadOnlyCollection<NativeAppRelease> GetReleases() {
		return _cache.GetOrCreate("NativeApps:Releases", entry => {
			entry.SlidingExpiration = TimeSpan.FromDays(100);
			return GetReleasesInternal();
		}) ?? GetReleasesInternal();
	}

	public void ClearCache() {
		_cache.Remove("NativeApps:Releases");
	}

	private IReadOnlyCollection<NativeAppRelease> GetReleasesInternal() {
		var results = new List<NativeAppRelease>();
		using var cryptoProvider = SHA1.Create();
		var artifactsDir = new DirectoryInfo(_settings.ArtifactsPath);
		if (!artifactsDir.Exists) {
			artifactsDir.Create();
		}
		foreach (var directory in artifactsDir.EnumerateDirectories()) {
			if (!Version.TryParse(directory.Name, out var version)) {
				continue;
			}
			var changeLog = directory.GetFiles(_changelogFileName).FirstOrDefault()?.ReadAllText() ?? string.Empty;
			var artifacts = new List<NativeAppReleaseArtifact>();
			var appRelease = new NativeAppRelease(version, changeLog);
			foreach (var platformDir in directory.EnumerateDirectories()) {
				if (!Enum.TryParse<NativeAppPlatform>(platformDir.Name, out var platform)) {
					continue;
				}
				foreach (var archDir in platformDir.EnumerateDirectories()) {
					if (!Enum.TryParse<NativeAppArchitecture>(archDir.Name, out var architecture)) {
						continue;
					}
					foreach (var file in archDir.EnumerateFiles()) {
						var artifact = new NativeAppReleaseArtifact(appRelease, platform, architecture, file.Name, file.CreationTimeUtc);
						if (platform == NativeAppPlatform.Win32) {
							artifact.FileLength = file.Length;
							using var fileStream = file.OpenRead();
							artifact.Sha1 = BitConverter.ToString(cryptoProvider.ComputeHash(fileStream))
								.Replace("-", "");
						}
						artifacts.Add(artifact);
					}
				}
			}
			appRelease.Artifacts = artifacts.ToImmutableList();
			results.Add(appRelease);
		}
		return results;
	}

	public async Task WriteArtifact(Version version, NativeAppPlatform platform, 
			NativeAppArchitecture architecture, string? changes, string fileName, Stream stream) {
		var versionRoot = new DirectoryInfo(Path.Combine(_settings.ArtifactsPath, version.ToString()));
		if (!versionRoot.Exists) {
			versionRoot.Create();
			var changeLogFile = new FileInfo(Path.Combine(versionRoot.FullName, _changelogFileName));
			if (!changeLogFile.Exists && !string.IsNullOrWhiteSpace(changes)) {
				await File.WriteAllTextAsync(changeLogFile.FullName, changes);
			}
		}
		var root = Path.Combine(versionRoot.FullName, platform.ToString(), architecture.ToString());
		if (!Directory.Exists(root)) {
			Directory.CreateDirectory(root);
		}
		var fileInfo = new FileInfo(Path.Combine(root, Path.GetFileName(fileName)));
		if (fileInfo.Exists) {
			_logger.LogWarning("File {FileName} already exists, removing", fileInfo.Name);
			fileInfo.Delete();
		}
		await using var file = fileInfo.OpenWrite();
		await stream.CopyToAsync(file);
	}

	public string? GetWinReleases() {
		var releases = GetReleases().SelectMany(x =>
			x.Artifacts.Where(a => a.Platform == NativeAppPlatform.Win32 &&
					".nupkg".Equals(Path.GetExtension(a.FileName), StringComparison.OrdinalIgnoreCase))
				.Select(a => new { Release = x, Artifact = a })).ToList();
		return releases.Any() ? string.Join(Environment.NewLine,
			releases.Select(r => $"{r.Artifact.Sha1} {r.Artifact.FileName} {r.Artifact.FileLength}")) : null;
	}

	public Stream ReadFile(NativeAppPlatform platform, NativeAppArchitecture architecture, Version version, 
			string fileName) {
		var fileInfo = GetFileInfo(platform, architecture, version, fileName);
		return fileInfo.OpenRead();
	}

	private FileInfo GetFileInfo(NativeAppPlatform platform, NativeAppArchitecture architecture, Version version,
		string fileName) {
		fileName = Path.GetFileName(fileName);
		var versionRoot = new DirectoryInfo(Path.Combine(_settings.ArtifactsPath, version.ToString()));
		var root = Path.Combine(versionRoot.FullName, platform.ToString(), architecture.ToString());
		var fileInfo = new FileInfo(Path.Combine(root, Path.GetFileName(fileName)));
		return fileInfo;
	}

	public (NativeAppRelease, NativeAppReleaseArtifact)? GetNewMacRelease(Version version,
			NativeAppArchitecture architecture) {
		var releases = GetReleases();
		var release = releases.Where(x => x.Version > version).MaxBy(x => x.Version);
		var macArtifact = release?.Artifacts.FirstOrDefault(a =>
			a.Platform == NativeAppPlatform.Darwin && a.Architecture == architecture);
		if (release == null || macArtifact == null) {
			return null;
		}
		return (release, macArtifact);
	}

	public Task Remove(NativeAppReleaseArtifact artifact) {
		var fileInfo = GetFileInfo(artifact.Platform, artifact.Architecture, artifact.Release.Version, artifact.FileName);
		var newName = Path.Combine(fileInfo.Directory.Parent.FullName, $"{fileInfo.Directory.Name}_removed");
		Directory.Move(fileInfo.Directory.FullName, newName);
		ClearCache();
		return Task.CompletedTask;
	}
}
