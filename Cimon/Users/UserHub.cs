using Cimon.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Hubs;

using System.Security.Principal;

public interface IUserClientApi
{
	Task NotifyWithUrl(string url, string message);
}

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
public class UserHub : Hub<IUserClientApi>
{
	private readonly ILogger _logger;

	public UserHub(ILogger<UserHub> logger) {
		_logger = logger;
	}

	public override async Task OnConnectedAsync() {
		await base.OnConnectedAsync();
		IIdentity identity = Context.User.Identity;
		var userName = identity?.Name;
		if (!string.IsNullOrWhiteSpace(userName)) {
			await Groups.AddToGroupAsync(Context.ConnectionId, userName!);
			var team = Context.User.Claims.FirstOrDefault(c => c.Type == TokenService.TeamClaimName)?.Value;
			if (team != null) {
				await Groups.AddToGroupAsync(Context.ConnectionId, team);
			}
		}
		_logger.LogInformation("User {Identifier} ({Name} {IsAuthenticated}) connected", Context.UserIdentifier,
			identity?.Name, identity?.IsAuthenticated);
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		await base.OnDisconnectedAsync(exception);
		IIdentity identity = Context.User.Identity;
		_logger.LogInformation("User {Identifier} ({Name} {IsAuthenticated}) disconnected", Context.UserIdentifier,
			identity?.Name, identity?.IsAuthenticated);
	}
}
