using System.Net;
using Microsoft.AspNetCore.Authorization;

namespace Cimon.Auth;

public class LocalhostRequirement : IAuthorizationRequirement
{
}

public class LocalhostRequirementHandler : AuthorizationHandler<LocalhostRequirement>
{
	protected override Task HandleRequirementAsync(AuthorizationHandlerContext context,
		LocalhostRequirement requirement) {
		var httpContext = context.Resource as HttpContext;
		if (httpContext?.Connection.RemoteIpAddress is null) return Task.CompletedTask;
		var remoteIp = httpContext.Connection.RemoteIpAddress;
		if (IPAddress.IsLoopback(remoteIp)) {
			context.Succeed(requirement);
		}
		return Task.CompletedTask;
	}
}
