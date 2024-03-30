using System.Buffers;
using Cimon.Contracts;
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
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> Token() {
		var user = await _userManager.GetUser(User);
		if (user.IsGuest()) {
			return BadRequest();
		}
		string token = _tokenService.CreateToken(user, user.Claims);
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

	[Route("retry-autologin")]
	public async Task<IActionResult> RetryAutologin() {
		Response.Cookies.Append(AllowNegotiateCookieName, "true");
		return Redirect("autologin");
	}

	public static string AllowNegotiateCookieName => "negotiate-reset";

	[Route("autologin")]
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> Autologin(string returnUrl, [FromServices] ICurrentUserAccessor userAccessor) {
		return await DoAutologin(returnUrl, userAccessor);
	}

	[Route("robotAutologin")]
	[Authorize(AuthenticationSchemes = $"{CookieAuthenticationDefaults.AuthenticationScheme},{NegotiateDefaults.AuthenticationScheme}")]
	public async Task<IActionResult> RobotAutologin(string returnUrl, [FromServices] ICurrentUserAccessor userAccessor) {
		return await DoAutologin(returnUrl, userAccessor);
	}

	private async Task<IActionResult> DoAutologin(string returnUrl, ICurrentUserAccessor userAccessor) {
		var userName = User.Identity?.Name?.ToLowerInvariant()!;
		var user = await userAccessor.Current;
		if (!user.IsGuest() && User.Identities.Any(i => i.AuthenticationType == CookieAuthenticationDefaults.AuthenticationScheme)) {
			return string.IsNullOrWhiteSpace(returnUrl) ? Ok() : LocalRedirect(returnUrl);
		}
		var name = User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
		return await SignInUsingCookie(returnUrl, name ?? userName);
	}

	[HttpPost]
	[Route("login")]
	public async Task<IActionResult> Login([FromForm]string userName, [FromForm]string password) {
		var loginResult = await _userManager.SignInAsync(userName, password);
		if (!loginResult) {
			Response.Headers["login-failed"] = true.ToString();
			return Redirect("/Login?error");
		}
		return await SignInUsingCookie("/api/users/openLastMonitor", userName);
	}

	private async Task<IActionResult> SignInUsingCookie(string returnUrl, string userName) {
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
		Response.Headers["logged-in"] = "true";
		if (!string.IsNullOrWhiteSpace(returnUrl)) {
			return LocalRedirect(returnUrl);
		}
		return Ok();
	}

	private static readonly HashSet<string> ActionsWithAllowedRedirects = new(StringComparer.OrdinalIgnoreCase) {
		"login",
		"autologin"
	};
	private static bool CheckIfCanRedirect(HttpRequest request) {
		if (!request.RouteValues.TryGetValue("controller", out var controller) ||
				controller?.ToString()?.ToLowerInvariant() != "auth") {
			return false;
		}
		return request.RouteValues.TryGetValue("action", out var action) &&
			ActionsWithAllowedRedirects.Contains(action?.ToString() ?? string.Empty);
	}
	public static Task<bool> TryHandleOnAuthenticationFailed(
		Microsoft.AspNetCore.Authentication.Negotiate.AuthenticationFailedContext context) {
		if (!CheckIfCanRedirect(context.Request)) {
			return Task.FromResult(false);
		}
		if (context.Request.Cookies.TryGetValue(AllowNegotiateCookieName, out _)) {
			context.Response.Cookies.Delete(AllowNegotiateCookieName);
			return Task.FromResult(true);
		}
		context.Response.Redirect("/login?error=autologinFailed");
		context.HandleResponse();
		return Task.FromResult(true);
	}
	public static Task<bool> TryHandleOnAuthenticated(AuthenticatedContext context) {
		if (!CheckIfCanRedirect(context.Request)) {
			return Task.FromResult(false);
		}
		if (context.HttpContext.User.Identity?.IsAuthenticated is not true) {
			context.Response.Redirect("/login?error=autologinFailed");
			context.Fail("");
		}
		return Task.FromResult(true);
	}

	public static Task<bool> TryHandleOnChallenge(ChallengeContext context) {
		if (!CheckIfCanRedirect(context.Request)) {
			return Task.FromResult(false);
		}
		if (context.Request.Headers.Authorization.Count == 0) {
			context.Response.OnStarting(() => {
				if (context.HttpContext.User.Identity?.IsAuthenticated is not true) {
					var cookieKey = "negotiate-try-attempt";
					if (context.Request.Cookies.ContainsKey(cookieKey)) {
						context.Response.Cookies.Delete(cookieKey);
						context.Response.Redirect("/login?error=autologinFailed");
					} else {
						context.Response.Cookies.Append(cookieKey, "true");
					}
				}
				return Task.CompletedTask;
			});
		}
		return Task.FromResult(true);
	}
}
