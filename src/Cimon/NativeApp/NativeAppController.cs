using Microsoft.AspNetCore.Mvc;

namespace Cimon.NativeApp;

[Route("api/native")]
public class NativeAppController : Controller
{
	private readonly NativeAppService _nativeAppService;

	public NativeAppController(NativeAppService nativeAppService) {
		_nativeAppService = nativeAppService;
	}

	[HttpGet]
	[Route("download/{version}/{platform}/{fileName?}")]
	public IActionResult Download(string version, string platform, string? fileName = null) {
		if (!Version.TryParse(version, out var parsedVersion)) {
			return NotFound();
		}
		if (!Enum.TryParse<NativeAppPlatform>(platform, out var parsedPlatform)) {
			return NotFound();
		}
		var contentType = parsedPlatform switch {
			_ => "application/vnd.microsoft.portable-executable"
		};
		return File(_nativeAppService.ReadArtifact(parsedVersion, parsedPlatform, fileName), contentType);
	}
}