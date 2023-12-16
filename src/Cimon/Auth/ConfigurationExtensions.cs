using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Cimon.Data.Users;
using System.Security.Principal;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

namespace Cimon.Auth;

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
						jwtSecurityToken.Payload.TryGetValue(ClaimTypes.NameIdentifier,
							out object? name) && await context.HttpContext.RequestServices.GetRequiredService<UserManager>()
								.IsDeactivated(name?.ToString())) {
						context.Fail("Token not active");
					}
				},
				OnMessageReceived = context => {
					var accessToken = context.Request.Query["access_token"].ToString();
					PathString path = context.HttpContext.Request.Path;
					if (accessToken is {Length:>0} &&
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
					IIdentity? identity = httpContext.User.Identity;
					if (identity?.Name is { Length: > 0 } name
							&& await GetIsDeactivated(httpContext, name)) {
						await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
					}
					await func.Invoke(httpContext);
				});
				next(builder);
			};
		}

		private static async Task<bool> GetIsDeactivated(HttpContext httpContext, string name) {
			var userManager = httpContext.RequestServices.GetRequiredService<UserManager>();
			return await userManager.IsDeactivated(name);
		}
	}

	public static void AddAuth(this IServiceCollection services) {
		services.AddSingleton<TokenService>();
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
