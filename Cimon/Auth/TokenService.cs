using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace Cimon.Auth;

using Microsoft.Extensions.Options;

public class TokenService
{
	internal const string TeamClaimName = "team";
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

	public List<Claim> CreateClaims(IdentityUser user, string team = "all") {
		var userName = user.UserName ?? user.Id;
		var claims = new List<Claim> {
			new(JwtRegisteredClaimNames.Sub, userName),
			new(JwtRegisteredClaimNames.Jti, user.Id),
			new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
			new(ClaimTypes.NameIdentifier, user.Id),
			new(ClaimTypes.Name, userName),
			new(TeamClaimName, team)
		};
		return claims;
	}

	private SigningCredentials CreateSigningCredentials() => 
		new(new SymmetricSecurityKey(_options.Jwt.Key), SecurityAlgorithms.HmacSha256);
}
