using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Cimon.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Primitives;

public static class Extensions
{
	class JwtConfigurator: IConfigureNamedOptions<JwtBearerOptions>
	{

		private readonly JwtOptions _jwtOptions;
		private readonly UserManager _userManager;

		public JwtConfigurator(JwtOptions jwtOptions, UserManager userManager) {
			_jwtOptions = jwtOptions;
			_userManager = userManager;
		}

		public void Configure(JwtBearerOptions options) {
			options.Events = new JwtBearerEvents {
				OnTokenValidated = context => {
					if (_userManager.IsDeactivated(context.SecurityToken)) {
						context.Fail("Token not active");
					}
					return Task.CompletedTask;
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
			parameters.ValidIssuer = _jwtOptions.Issuer;
			parameters.ValidAudience = _jwtOptions.Audience;
			parameters.IssuerSigningKey = new SymmetricSecurityKey(_jwtOptions.Key);
		}
		public void Configure(string? name, JwtBearerOptions options) => Configure(options);
	}

	class UserManagerFilter: IStartupFilter
	{

		private readonly UserManager _userManager;

		public UserManagerFilter(UserManager userManager) {
			_userManager = userManager;
		}

		public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) {
			return builder => {
				builder.Use(async (httpContext, func) => {
					if (_userManager.IsDeactivated(httpContext.User)) {
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
		services.AddSingleton<UserManager, UserManager>();
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddNegotiate()
			.AddCookie()
			.AddJwtBearer();
		services.AddTransient<IStartupFilter, UserManagerFilter>();
		services.AddTransient<IConfigureOptions<JwtBearerOptions>, JwtConfigurator>();
	}

}
