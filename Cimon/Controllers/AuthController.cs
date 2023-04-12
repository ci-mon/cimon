namespace Cimon.Controllers;

using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Route("auth")]
[Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]
public class AuthController : Controller
{
	public async Task<IActionResult> Get() {
		return Ok(new {
			userName = User.Identity?.Name
		});
	}
}
