using System.Security.Claims;
using Cimon.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;

namespace Cimon.Controllers;

using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

class AuthResponse
{
	public string UserName { get; set; }
	public string Token { get; set; }
}

[Route("auth")]
public class AuthController : Controller
{
	private readonly TokenService _tokenService;

	public AuthController(TokenService tokenService) {
		_tokenService = tokenService;
	}

	[Route("checkJwt")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<IActionResult> CheckJwt() {
		return Ok(User.Identity.Name);
	}

	[Route("autologin")]
	[Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]
	public async Task<IActionResult> Autologin(string returnUrl) {
		var identityUser = new IdentityUser(User.Identity.Name);
		var claimsIdentity = new ClaimsIdentity(
			_tokenService.CreateClaims(identityUser), 
			CookieAuthenticationDefaults.AuthenticationScheme);
		var authProperties = new AuthenticationProperties {
			AllowRefresh = true,
			ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10),
			IsPersistent = true,
			RedirectUri = "/auth/autologin"
		};
		await HttpContext.SignInAsync(
			CookieAuthenticationDefaults.AuthenticationScheme, 
			new ClaimsPrincipal(claimsIdentity), 
			authProperties);
		return LocalRedirect(returnUrl);
	}

	[Route("token")]
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> Token() {
		var identityUser = new IdentityUser(User.Identity.Name);
		var token = _tokenService.CreateToken(identityUser);
		return Ok(new AuthResponse { UserName = User.Identity.Name, Token = token });
	}
	[HttpPost]
	[Route("login")]
	public async Task<IActionResult> Login([FromForm]string userName, [FromForm]string password) {
		ControllerContext.HttpContext.Response.Cookies.Append("auth", "XXX");
		return LocalRedirect("/");
	}
}
