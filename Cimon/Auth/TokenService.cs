using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cimon.Contracts;
using Cimon.Data.Users;
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

	public string CreateToken(User user) {
		var claims = CreateClaims(user);
		JwtSecurityToken token = CreateJwtToken(claims);
		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.WriteToken(token);
	}

	private JwtSecurityToken CreateJwtToken(List<Claim> claims) =>
		new(_options.Jwt.Issuer, _options.Jwt.Audience, claims,
			expires: DateTime.UtcNow + _options.Auth.Expiration,
			signingCredentials: _signingCredentials);

	public List<Claim> CreateClaims(User userInfo) {
		var userName = userInfo.Name.Name;
		var claims = new List<Claim> {
			new(JwtRegisteredClaimNames.Sub, userName),
			new(JwtRegisteredClaimNames.Jti, userInfo.Id.Id),
			new(JwtRegisteredClaimNames.Iat, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture)),
			new(ClaimTypes.NameIdentifier, userInfo.Id.Id),
			new(ClaimTypes.Name, userName),
			new(ClaimTypes.Email, userInfo.Email),
		};
		claims.AddRange(userInfo.Teams?.Select(x => new Claim(TeamClaimName, x)) ?? Array.Empty<Claim>());
		claims.AddRange(userInfo.Roles?.Select(x => new Claim(ClaimTypes.Role, x)) ?? Array.Empty<Claim>());
		return claims;
	}

	private SigningCredentials CreateSigningCredentials() => 
		new(new SymmetricSecurityKey(_options.Jwt.Key), SecurityAlgorithms.HmacSha256);
}
