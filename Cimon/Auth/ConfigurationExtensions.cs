using System.IdentityModel.Tokens.Jwt;

namespace Cimon.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

public static class ConfigurationExtensions
{
	class AuthConfigurator: IConfigureNamedOptions<JwtBearerOptions>, IConfigureNamedOptions<CookieAuthenticationOptions>
	{

		private readonly CimonOptions _options;
		public AuthConfigurator(IOptions<CimonOptions> options) {
			_options = options.Value;
		}

		public void Configure(JwtBearerOptions options) {
			options.Events = new JwtBearerEvents {
				OnTokenValidated = async context => {
					if (context.SecurityToken is JwtSecurityToken jwtSecurityToken &&
						jwtSecurityToken.Payload.TryGetValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
							out object? name) && await context.HttpContext.RequestServices.GetRequiredService<UserManager>()
								.IsDeactivated(name?.ToString())) {
						context.Fail("Token not active");
					}
				},
				OnMessageReceived = context => {
					StringValues accessToken = context.Request.Query["access_token"];
					PathString path = context.HttpContext.Request.Path;
					if (!string.IsNullOrEmpty(accessToken) &&
						path.StartsWithSegments("/hubs")) {
						context.Token = accessToken;
					}
					return Task.CompletedTask;
				}
			};
			TokenValidationParameters parameters = options.TokenValidationParameters;
			parameters.ClockSkew = TimeSpan.Zero;
			parameters.ValidateIssuer = true;
			parameters.ValidateAudience = true;
			parameters.ValidateLifetime = true;
			parameters.ValidateIssuerSigningKey = true;
			parameters.ValidIssuer = _options.Jwt.Issuer;
			parameters.ValidAudience = _options.Jwt.Audience;
			parameters.IssuerSigningKey = new SymmetricSecurityKey(_options.Jwt.Key);
		}

		public void Configure(string? name, JwtBearerOptions options) => Configure(options);

		public void Configure(CookieAuthenticationOptions options) {
			options.ReturnUrlParameter = "returnUrl";
			options.LoginPath = "/Login";
			options.LogoutPath = "/auth/logout";
			options.ExpireTimeSpan = _options.Auth.Expiration;
		}

		public void Configure(string? name, CookieAuthenticationOptions options) => Configure(options);
	}

	class UserManagerFilter: IStartupFilter
	{

		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
			return builder => {
				builder.Use(async (httpContext, func) => {
					if (httpContext.User.Identity?.Name is { Length: > 0 } name 
							&& await httpContext.RequestServices.GetRequiredService<UserManager>()
								.IsDeactivated(name)) {
						await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
					}
					await func.Invoke(httpContext);
				});
				next(builder);
			};
		}
	}

	public static void AddAuth(this IServiceCollection services) {
		services.AddSingleton<TokenService, TokenService>();
		services.AddScoped<UserManager, UserManager>();
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddNegotiate()
			.AddCookie()
			.AddJwtBearer();
		services.AddAuthorization(options => {
			options.AddPolicy("LocalhostPolicy", policy =>
				policy.Requirements.Add(new LocalhostRequirement()));
		});
		services.AddTransient<IStartupFilter, UserManagerFilter>();
		services.AddTransient<IConfigureOptions<JwtBearerOptions>, AuthConfigurator>();
		services.AddTransient<IConfigureOptions<CookieAuthenticationOptions>, AuthConfigurator>();
		services.AddSingleton<IAuthorizationHandler, LocalhostRequirementHandler>();
	}

}
