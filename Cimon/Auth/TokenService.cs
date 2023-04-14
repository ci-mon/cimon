using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Cimon.Auth;

public class TokenService
{

	private readonly SigningCredentials _signingCredentials;
	private readonly JwtOptions _jwtOptions;

	public TokenService(JwtOptions jwtOptions) {
		_jwtOptions = jwtOptions;
		_signingCredentials = CreateSigningCredentials();
	}

	public string CreateToken(IdentityUser user) {
		var claims = CreateClaims(user);
		JwtSecurityToken token = CreateJwtToken(claims);
		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.WriteToken(token);
	}

	private JwtSecurityToken CreateJwtToken(List<Claim> claims) =>
		new(_jwtOptions.Issuer, _jwtOptions.Audience, claims, expires: DateTime.UtcNow + _jwtOptions.Expiration,
			signingCredentials: _signingCredentials);

	public List<Claim> CreateClaims(IdentityUser user) {
		var claims = new List<Claim> {
			new(JwtRegisteredClaimNames.Sub, "cimon_token"),
			new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
			new(ClaimTypes.NameIdentifier, user.Id),
			new(ClaimTypes.Name, user.UserName ?? user.Id)
		};
		return claims;
	}

	private SigningCredentials CreateSigningCredentials() => 
		new(new SymmetricSecurityKey(_jwtOptions.Key), SecurityAlgorithms.HmacSha256);
}
