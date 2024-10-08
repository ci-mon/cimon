﻿using System.Buffers;
using Cimon.Contracts;
using Cimon.Data.Secrets;
using Cimon.Data.Users;

namespace Cimon.Auth;

using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

[Route("auth")]
public class AuthController : Controller
{
	private readonly TokenService _tokenService;
	private readonly UserManager _userManager;
	private readonly CimonSecrets _secrets;

	public AuthController(TokenService tokenService, UserManager userManager, IOptions<CimonSecrets> options) {
		_tokenService = tokenService;
		_userManager = userManager;
		_secrets = options.Value;
	}

	[Route("token")]
	[Authorize(AuthenticationSchemes =
		$"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> Token() {
		var user = await _userManager.GetUser(User);
		if (user.IsGuest()) {
			return BadRequest();
		}
		string token = _tokenService.CreateToken(user, user.Claims);
		return Ok(new AuthResponse { UserName = user.FullName, Token = token });
	}

	[Route("checkToken")]
	[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
	public IActionResult CheckToken() {
		return Ok(User.Identity?.Name);
	}

	[Route("logoutUser")]
	[HttpPost]
	[Authorize(AuthenticationSchemes =
		$"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme},{JwtBearerDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> DeactivateUser([FromBody] SignOutRequest req) {
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

	[Route("retry-autologin")]
	public IActionResult RetryAutologin() {
		Response.Cookies.Append(AllowNegotiateCookieName, "true");
		return Redirect("autologin");
	}

	public static string AllowNegotiateCookieName => "negotiate-reset";

	[Route("autologin")]
	[Authorize(AuthenticationSchemes =
		$"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> Autologin(string returnUrl, [FromServices] ICurrentUserAccessor userAccessor) {
		var userName = User.Identity?.Name?.ToLowerInvariant()!;
		var user = await userAccessor.Current;
		if (!user.IsGuest() &&
			User.Identities.Any(i => i.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme)) {
			return string.IsNullOrWhiteSpace(returnUrl) ? Ok() : LocalRedirect(returnUrl);
		}
		var name = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
		return await SignInUsingCookie(returnUrl, name ?? userName);
	}

	[HttpPost]
	[Route("login")]
	public async Task<IActionResult> Login([FromForm] string userName, [FromForm] string password,
			[FromForm] string? returnUrl) {
		var loginResult = await _userManager.SignInAsync(userName, password);
		if (!loginResult) {
			Response.Headers["login-failed"] = true.ToString();
			return Redirect("/Login?error");
		}
		return await SignInUsingCookie(returnUrl, userName);
	}

	private async Task<IActionResult> SignInUsingCookie(string? returnUrl, string userName) {
		var user = await _userManager.FindOrCreateUser(userName);
		if (user == null) {
			return BadRequest();
		}
		var claimsIdentity = new ClaimsIdentity(user.Claims, CookieAuthenticationDefaults.AuthenticationScheme);
		var authProperties = new AuthenticationProperties {
			AllowRefresh = true,
			ExpiresUtc = DateTimeOffset.UtcNow.Add(_secrets.Auth.Expiration),
			IsPersistent = true,
			RedirectUri = "/auth/autologin",
		};
		await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
			new ClaimsPrincipal(claimsIdentity), authProperties);
		if (!string.IsNullOrWhiteSpace(returnUrl)) {
			return LocalRedirect(returnUrl);
		}
		return Ok();
	}
}
