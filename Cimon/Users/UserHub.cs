using Cimon.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Principal;
using Cimon.Data;
using Cimon.Data.Discussions;
using MediatR;

namespace Cimon.Hubs;

public interface IUserClientApi
{
	Task NotifyWithUrl(string buildId, string url, string header, string message);
}

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
public class UserHub : Hub<IUserClientApi>
{
	private readonly ILogger _logger;
	private readonly IMediator _mediator;

	public UserHub(ILogger<UserHub> logger, IMediator mediator) {
		_logger = logger;
		_mediator = mediator;
	}

	public override async Task OnConnectedAsync() {
		await base.OnConnectedAsync();
		var identity = Context.User?.Identity;
		var userName = identity?.Name;
		if (!string.IsNullOrWhiteSpace(userName)) {
			await Groups.AddToGroupAsync(Context.ConnectionId, userName!);
			var team = Context.User?.Claims.FirstOrDefault(c => c.Type == TokenService.TeamClaimName)?.Value;
			if (team != null) {
				await Groups.AddToGroupAsync(Context.ConnectionId, team);
			}
		}
		_logger.LogInformation("User {Identifier} ({Name} {IsAuthenticated}) connected", Context.UserIdentifier,
			identity?.Name, identity?.IsAuthenticated);
	}

	public override async Task OnDisconnectedAsync(Exception? exception) {
		await base.OnDisconnectedAsync(exception);
		var identity = Context.User?.Identity;
		_logger.LogInformation("User {Identifier} ({Name} {IsAuthenticated}) disconnected", Context.UserIdentifier,
			identity?.Name, identity?.IsAuthenticated);
	}

	public async Task ReplyToNotification(string buildId, QuickReplyType quickReplyType, string comment) {
		await _mediator.Publish(new AddCommentNotification {
			BuildId = buildId,
			QuickReplyType = quickReplyType,
			Comment = comment
		});
	}

}

