using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Cimon.NativeApp;

public class NativeAppSecrets
{
	public string PublishApiKey { get; set; } = "changeme";
}

[Route("native")]
public class NativeAppController : Controller
{
	private readonly NativeAppService _nativeAppService;
	private readonly IMediator _mediator;
	private readonly NativeAppSecrets _secrets;

	public NativeAppController(NativeAppService nativeAppService, IMediator mediator, 
			IOptions<NativeAppSecrets> secrets) {
		_nativeAppService = nativeAppService;
		_mediator = mediator;
		_secrets = secrets.Value;
	}

	[HttpGet]
	[Route("update/{callbackUrl}/{platform}/{arch}/{version}/{fileName?}")]
	public async Task<IActionResult> CheckForUpdate(string callbackUrl, NativeAppPlatform platform, 
			NativeAppArchitecture arch, Version version, string? fileName = null) {
		switch (platform) {
			case NativeAppPlatform.Win32 when "RELEASES".Equals(fileName, StringComparison.OrdinalIgnoreCase): {
				var releases = _nativeAppService.GetWinReleases();
				if (releases is null) {
					return NoContent();
				}
				return Content(releases, "text/plain");
			}
			case NativeAppPlatform.Win32 when fileName is null: {
				return NoContent();
			}
			case NativeAppPlatform.Darwin when string.IsNullOrWhiteSpace(fileName): {
				var newRelease = _nativeAppService.GetNewMacRelease(version, arch);
				if (newRelease == null) {
					return NoContent();
				}
				var release = newRelease.Value.Item1;
				var artifact = newRelease.Value.Item2;
				var baseUrl = Encoding.UTF8.GetString(Convert.FromBase64String(callbackUrl));
				return Ok(new {
					name = $"cimon-desktop {release.Version}",
					url = $"{baseUrl}/update/{callbackUrl}/{platform}/{arch}/{version}/{artifact.FileName}",
					notes = release.ReleaseNotes,
					pub_date = artifact.CreatedOn.ToString("s")
				});
			}
		}
		if (platform == NativeAppPlatform.Win32) {
			var requiredVersion = Regex.Match(fileName, @"-(?'version'\d{1,}.\d{1,}.\d{1,}(.\d){0,})-")
				.Groups["version"].Value;
			Version.TryParse(requiredVersion, out version);
		}
		var fileStream = _nativeAppService.ReadFile(platform, arch, version, fileName);
		var contentType = Path.GetExtension(fileName) switch {
			".zip" or ".nupkg" => "application/zip",
			_ => "application/octet-stream"
		};
		return File(fileStream, contentType);
	}

	[HttpPost]
	[Route("upload/{appId}")]
	[RequestSizeLimit(500_000_000)]
	public async Task<IActionResult> Upload(string appId, IFormCollection form) {
		var apiKey = Request.Headers["Authorization"];
		if (string.IsNullOrWhiteSpace(_secrets.PublishApiKey) && !_secrets.PublishApiKey.Equals(apiKey)) {
			return Unauthorized();
		}
		var platformDesc = form["platform"].FirstOrDefault();
		var archDesc = form["arch"].FirstOrDefault();
		var versionDesc = form["version"].FirstOrDefault();
		var changes = form["changes"].FirstOrDefault();
		ArgumentException.ThrowIfNullOrEmpty(platformDesc);
		ArgumentException.ThrowIfNullOrEmpty(archDesc);
		ArgumentException.ThrowIfNullOrEmpty(versionDesc);
		var platform = Enum.Parse<NativeAppPlatform>(platformDesc, true);
		var arch = Enum.Parse<NativeAppArchitecture>(archDesc, true);
		var version = Version.Parse(versionDesc);
		foreach (var file in form.Files) {
			await using var stream = file.OpenReadStream();
			await _nativeAppService.WriteArtifact(version, platform, arch, changes, file.FileName, stream);
		}
		_nativeAppService.ClearCache();
		await _mediator.Publish(new NativeAppPublished());
		return Ok();
	}

}