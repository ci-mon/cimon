using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Cimon.Auth;

using Microsoft.Extensions.Options;

public class TokenService
{
	private readonly CimonOptions _options;
	private readonly SigningCredentials _signingCredentials;

	public TokenService(IOptions<CimonOptions> cimonOptions) {
		_options = cimonOptions.Value;
		_signingCredentials = CreateSigningCredentials();
	}

	public string CreateToken(IdentityUser user) {
		var claims = CreateClaims(user);
		JwtSecurityToken token = CreateJwtToken(claims);
		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.WriteToken(token);
	}

	private JwtSecurityToken CreateJwtToken(List<Claim> claims) =>
		new(_options.Jwt.Issuer, _options.Jwt.Audience, claims,
			expires: DateTime.UtcNow + _options.Auth.Expiration,
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
		new(new SymmetricSecurityKey(_options.Jwt.Key), SecurityAlgorithms.HmacSha256);
}
