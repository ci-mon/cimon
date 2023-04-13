using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cimon.Auth;

public static class Extensions
{
	public static void AddAuth(this IServiceCollection services) {
		services.AddSingleton<TokenService, TokenService>();
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddNegotiate()
			.AddCookie()
			.AddJwtBearer(options => {
				options.Events = new JwtBearerEvents() {
					OnMessageReceived = context => {
						var accessToken = context.Request.Query["access_token"];
						var path = context.HttpContext.Request.Path;
						if (!string.IsNullOrEmpty(accessToken) &&
						    path.StartsWithSegments("/hubs")) {
							context.Token = accessToken;
						}
						return Task.CompletedTask;
					}
				};
				options.TokenValidationParameters = new TokenValidationParameters {
					ClockSkew = TimeSpan.Zero,
					ValidateIssuer = true,
					ValidateAudience = true,
					ValidateLifetime = true,
					ValidateIssuerSigningKey = true,
					ValidIssuer = "apiWithAuthBackend",
					ValidAudience = "apiWithAuthBackend",
					IssuerSigningKey = new SymmetricSecurityKey(
						TokenService.Key
					),
				};
			});
	}
}
