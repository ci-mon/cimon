namespace Cimon.Auth;

using System.DirectoryServices.Protocols;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

public class UserManager
{
	private readonly HashSet<string> _revokedUsers = new(StringComparer.OrdinalIgnoreCase);
	private readonly TokenService _tokenService;
	private readonly ILogger _logger;

	public UserManager(TokenService tokenService, ILogger<UserManager> logger) {
		_tokenService = tokenService;
		_logger = logger;
	}

	public bool IsDeactivated(ClaimsPrincipal principal) =>
		principal.Identity?.Name is { Length: > 0 } name && _revokedUsers.Contains(name);

	public bool SignOut(string securityTokenId) => _revokedUsers.Add(securityTokenId);

	public string GetToken(ClaimsPrincipal principal) {
		var identityUser = new IdentityUser(principal.Identity.Name);
		return _tokenService.CreateToken(identityUser);
	}

	public bool IsDeactivated(SecurityToken contextSecurityToken) {
		if (contextSecurityToken is not JwtSecurityToken jwtSecurityToken) {
			return false;
		}
		return jwtSecurityToken.Payload.TryGetValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
			out object? name) && _revokedUsers.Contains(name);
	}

	public async Task<PasswordSignInResult> SignInAsync(UserName userName, string password) {
		var result = new PasswordSignInResult {
			UserName = userName,
			Success = true,
			Team = "all"
		};
		if ((userName == "test1" || userName == "test2") && password == "test") {
			result.Team = "testers";
			return result;
		}
		string domain = userName.Domain.ToLowerInvariant(); // TODO get from where?
		LdapConnection connection = new($"{domain}.com");
		NetworkCredential credential = new(userName.Name, password, userName.Domain);
		try {
			connection.Bind(credential);
			connection.Dispose();
			result.Success = true;
		} catch (Exception e) {
			_logger.LogWarning(e, "Error during user [{User}] auth", userName);
		}
		return result;
	}
}
