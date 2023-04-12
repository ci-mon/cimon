using Microsoft.AspNetCore.SignalR;

namespace Cimon.Hubs;

using System.Security.Principal;

public class UserHub : Hub
{
	private ILogger _logger;

	public UserHub(ILogger<UserHub> logger) {
		_logger = logger;
	}

	public override async Task OnConnectedAsync() {
		await base.OnConnectedAsync();
		IIdentity identity = Context.User.Identity;
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
