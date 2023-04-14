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
using Microsoft.Extensions.Options;

class AuthResponse
{
	public string UserName { get; set; }
	public string Token { get; set; }
}

public class SignOutRequest
{
	public string UserName { get; set; }
}


[Route("auth")]
public class AuthController : Controller
{
	private readonly TokenService _tokenService;
	private readonly UserManager _userManager;

	public AuthController(TokenService tokenService, UserManager userManager) {
		_tokenService = tokenService;
		_userManager = userManager;
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
		string token = _userManager.GetToken(User);
		return Ok(new AuthResponse { UserName = User.Identity.Name, Token = token });
	}

	[Route("checkToken")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public async Task<IActionResult> CheckToken() {
		return Ok(User.Identity.Name);
	}

	[Route("signOut")]
	[HttpPost]
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> SignOut([FromBody]SignOutRequest req) {
		_userManager.SignOut(req.UserName);
		return Ok();
	}

	[HttpPost]
	[Route("login")]
	public async Task<IActionResult> Login([FromForm]string userName, [FromForm]string password) {
		ControllerContext.HttpContext.Response.Cookies.Append("auth", "XXX");
		return LocalRedirect("/");
	}
}
