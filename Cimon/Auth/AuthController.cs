using System.Net;
using Cimon.Data.Users;

namespace Cimon.Auth;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Route("auth")]
public class AuthController : Controller
{
	private readonly TokenService _tokenService;
	private readonly UserManager _userManager;
	private readonly CimonOptions _options;

	public AuthController(TokenService tokenService, UserManager userManager, IOptions<CimonOptions> options) {
		_tokenService = tokenService;
		_userManager = userManager;
		_options = options.Value;
	}

	[Route("token")]
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> Token() {
		var user = await _userManager.FindOrCreateUser(User.Identity!.Name!);
		string token = _tokenService.CreateToken(user);
		return Ok(new AuthResponse { UserName = User.Identity!.Name!, Token = token });
	}

	[Route("checkToken")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public IActionResult CheckToken() {
		return Ok(User.Identity?.Name);
	}

	[Route("logoutUser")]
	[HttpPost]
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> DeactivateUser([FromBody]SignOutRequest req) {
		await _userManager.Deactivate(req.UserName);
		return Ok();
	}
	
	[Route("logout")]
	[HttpGet]
	[Authorize]
	public async Task<IActionResult> Logout() {
		await HttpContext.SignOutAsync();
		return Redirect("/");
	}

	[Route("autologin")]
	[Authorize(AuthenticationSchemes = NegotiateDefaults.AuthenticationScheme)]
	public async Task<IActionResult> Autologin(string returnUrl) {
		var userName = User.Identity?.Name?.ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(userName)) {
			return Unauthorized();
		}
		return await SignInUsingCookie(returnUrl, userName);
	}

	[HttpPost]
	[Route("login")]
	public async Task<IActionResult> Login([FromForm]string userName, [FromForm]string password) {
		var loginResult = await _userManager.SignInAsync(userName, password);
		if (!loginResult) {
			return Unauthorized();
		}
		return await SignInUsingCookie("/", userName);
	}

	private async Task<IActionResult> SignInUsingCookie(string returnUrl, string userName) {
		var user = await _userManager.FindOrCreateUser(userName);
		var claims = _tokenService.CreateClaims(user);
		var claimsIdentity = new ClaimsIdentity(claims,
			CookieAuthenticationDefaults.AuthenticationScheme);
		var authProperties = new AuthenticationProperties {
			AllowRefresh = true,
			ExpiresUtc = DateTimeOffset.UtcNow.Add(_options.Auth.Expiration),
			IsPersistent = true,
			RedirectUri = "/auth/autologin",
		};
		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
			new ClaimsPrincipal(claimsIdentity), authProperties);
		return LocalRedirect(returnUrl);
	}
}
