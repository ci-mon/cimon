using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace Cimon.Auth;

public class ConfigureJwtBearerOptions : IPostConfigureOptions<JwtBearerOptions>
{
	public void PostConfigure(string? name, JwtBearerOptions options) {
		var originalOnMessageReceived = options.Events?.OnMessageReceived;
		options.Events ??= new JwtBearerEvents();
		options.Events.OnMessageReceived = async context => {
			if (originalOnMessageReceived != null) {
				await originalOnMessageReceived(context);
			}
			if (string.IsNullOrEmpty(context.Token)) {
				var accessToken = context.Request.Query["access_token"];
				var path = context.HttpContext.Request.Path;
				if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs")) {
					context.Token = accessToken;
				}
			}
		};
	}
}
