using System.IdentityModel.Tokens.Jwt;
using System.Reactive.Linq;
using System.Security.Claims;
using Cimon.Data.Users;
using System.Security.Principal;
using Cimon.Data.Monitors;
using Cimon.DB.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using RouteData = Microsoft.AspNetCore.Components.RouteData;

namespace Cimon.Auth;

public static class ConfigurationExtensions
{
	class AuthConfigurator: IConfigureNamedOptions<JwtBearerOptions>, IConfigureNamedOptions<CookieAuthenticationOptions>
	{

		private readonly CimonSecrets _secrets;
		public AuthConfigurator(IOptions<CimonSecrets> options) {
			_secrets = options.Value;
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
			parameters.ValidIssuer = _secrets.Jwt.Issuer;
			parameters.ValidAudience = _secrets.Jwt.Audience;
			parameters.IssuerSigningKey = new SymmetricSecurityKey(_secrets.Jwt.Key);
		}

		public void Configure(string? name, JwtBearerOptions options) => Configure(options);

		public void Configure(CookieAuthenticationOptions options) {
			options.ReturnUrlParameter = "returnUrl";
			options.LoginPath = "/Login";
			options.LogoutPath = "/auth/logout";
			options.ExpireTimeSpan = _secrets.Auth.Expiration;
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
	
	public static IServiceCollection AddAuthorization(this IServiceCollection services,
		Action<AuthorizationOptions, IServiceProvider> configure) {
		services.AddOptions<AuthorizationOptions>().Configure<IServiceProvider>(configure);
		return services.AddAuthorization();
	}

	public static void AddAuth(this IServiceCollection services) {
		services.AddSingleton<TokenService>();
		services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
			.AddNegotiate()
			.AddCookie()
			.AddJwtBearer();
		services.AddAuthorization((options, sp) => {
			options.AddPolicy("LocalhostPolicy", policy =>
				policy.Requirements.Add(new LocalhostRequirement()));
			options.AddPolicy("EditMonitor", policy => policy.RequireAssertion(x => MonitorEditAssertions(x, sp)));
		});
		services.AddTransient<IStartupFilter, UserManagerFilter>();
		services.AddTransient<IConfigureOptions<JwtBearerOptions>, AuthConfigurator>();
		services.AddTransient<IConfigureOptions<CookieAuthenticationOptions>, AuthConfigurator>();
		services.AddSingleton<IAuthorizationHandler, LocalhostRequirementHandler>();
	}

	private static async Task<bool> MonitorEditAssertions(AuthorizationHandlerContext context, IServiceProvider sp) {
		if (context.User.IsInRole("monitor-editor")) {
			return true;
		}
		MonitorModel? monitor = null;
		switch (context.Resource) {
			case RouteData rd when rd.RouteValues.TryGetValue("monitorId", out var monitorId): {
				var monService = sp.GetRequiredService<MonitorService>();
				var id = Convert.ToString(monitorId, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
				monitor = await monService.GetMonitorById(id).Timeout(TimeSpan.FromSeconds(10)).FirstOrDefaultAsync();
				break;
			}
			case MonitorModel monitorModel:
				monitor = monitorModel;
				break;
		}
		if (monitor?.Owner?.Name is {Length:>0 } name) {
			return context.User.HasClaim(ClaimTypes.NameIdentifier, name);
		}
		return false;
	}
}
