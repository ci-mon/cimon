using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Internal;

[Route("api/internal")]
[Authorize(Policy = "LocalhostPolicy")]
public class InternalApiController : ControllerBase
{
	[HttpPost]
	[Route("emulateAllBuildsAreGreen")]
	public IActionResult EmulateAllBuildsAreGreen([FromQuery] bool value) {
		return NotFound();
	}
}
