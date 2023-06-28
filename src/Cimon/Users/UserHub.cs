using Cimon.Data.Discussions;
using Cimon.Data.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

public interface IUserClientApi
{
	Task NotifyWithUrl(string buildId, string url, string header, string message);
}

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
public class UserHub : Hub<IUserClientApi>
{
	private readonly ILogger _logger;
	private readonly IMediator _mediator;
	private readonly UserManager _userManager;

	public UserHub(ILogger<UserHub> logger, IMediator mediator, UserManager userManager) {
		_logger = logger;
		_mediator = mediator;
		_userManager = userManager;
	}

	public override async Task OnConnectedAsync() {
		await base.OnConnectedAsync();
		var identity = Context.User?.Identity;
		var user = await _userManager.GetUser(Context.User);
		await Groups.AddToGroupAsync(Context.ConnectionId, user.Name);
		foreach (var team in user.Teams) {
			await Groups.AddToGroupAsync(Context.ConnectionId, team);
		}
		_logger.LogInformation("[{HubId}] User {Identifier} ({Name} IsAuthenticated = {IsAuthenticated}) connected",
			Context.Items.GetHashCode(),
			Context.UserIdentifier, identity?.Name, identity?.IsAuthenticated);
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		await base.OnDisconnectedAsync(exception);
		var identity = Context.User?.Identity;
		_logger.LogInformation("[{HubId}] User {Identifier} ({Name} IsAuthenticated = {IsAuthenticated}) disconnected",
			Context.Items.GetHashCode(),
			Context.UserIdentifier, identity?.Name, identity?.IsAuthenticated);
	}

	public async Task ReplyToNotification(string buildId, QuickReplyType quickReplyType, string comment) {
		await _mediator.Publish(new AddReplyCommentNotification {
			BuildId = buildId,
			QuickReplyType = quickReplyType,
			Comment = comment
		});
	}

}

