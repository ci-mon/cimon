using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cimon.Contracts;
using Cimon.Data.Secrets;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Cimon.Auth;

public class TokenService
{
	private readonly CimonSecrets _secrets;
	private readonly SigningCredentials _signingCredentials;

	public TokenService(IOptions<CimonSecrets> cimonOptions) {
		_secrets = cimonOptions.Value;
		_signingCredentials = CreateSigningCredentials();
	}

	public string CreateToken(User user, IImmutableList<Claim> sourceClaims) {
		var claims = sourceClaims.AddRange(new [] {
			new Claim(JwtRegisteredClaimNames.Sub, user.Name.Name),
			new Claim(JwtRegisteredClaimNames.Jti, user.Name),
			new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
		});
		JwtSecurityToken token = CreateJwtToken(claims);
		var tokenHandler = new JwtSecurityTokenHandler();
		return tokenHandler.WriteToken(token);
	}

	private JwtSecurityToken CreateJwtToken(IEnumerable<Claim> claims) =>
		new(_secrets.Jwt.Issuer, _secrets.Jwt.Audience, claims,
			expires: DateTime.UtcNow + _secrets.Auth.Expiration,
			signingCredentials: _signingCredentials);


	private SigningCredentials CreateSigningCredentials() => 
		new(new SymmetricSecurityKey(_secrets.Jwt.Key), SecurityAlgorithms.HmacSha256);
}
