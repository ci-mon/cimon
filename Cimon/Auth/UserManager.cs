namespace Cimon.Auth;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

public class UserManager
{
	private readonly HashSet<string> _revokedUsers = new(StringComparer.OrdinalIgnoreCase);
	private readonly TokenService _tokenService;

	public UserManager(TokenService tokenService) {
		_tokenService = tokenService;
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
}
