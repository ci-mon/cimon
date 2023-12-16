using System.Collections.Immutable;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cimon.Contracts;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;

namespace Cimon.Auth;

public class TokenService
{
	private readonly CimonOptions _options;
	private readonly SigningCredentials _signingCredentials;

	public TokenService(IOptions<CimonOptions> cimonOptions) {
		_options = cimonOptions.Value;
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
		new(_options.Jwt.Issuer, _options.Jwt.Audience, claims,
			expires: DateTime.UtcNow + _options.Auth.Expiration,
			signingCredentials: _signingCredentials);


	private SigningCredentials CreateSigningCredentials() => 
		new(new SymmetricSecurityKey(_options.Jwt.Key), SecurityAlgorithms.HmacSha256);
}
