using Akka.Actor;
using Akka.Hosting;
using Cimon.Data;
using Cimon.Data.Discussions;
using Cimon.Data.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
public class UserHub : Hub<IUserClientApi>
{
	private readonly ILogger _logger;
	private readonly IMediator _mediator;
	private readonly UserManager _userManager;
	private readonly ICurrentUserAccessor _userAccessor;
	private readonly IRequiredActor<UserSupervisorActor> _userSupervisor;

	public UserHub(ILogger<UserHub> logger, IMediator mediator, UserManager userManager, 
			ICurrentUserAccessor userAccessor, IRequiredActor<UserSupervisorActor> userSupervisor) {
		_logger = logger;
		_mediator = mediator;
		_userManager = userManager;
		_userAccessor = userAccessor;
		_userSupervisor = userSupervisor;
	}

	public override async Task OnConnectedAsync() {
		await base.OnConnectedAsync();
		var identity = Context.User?.Identity;
		var user = await _userManager.GetUser(Context.User);
		_userSupervisor.ActorRef.Tell(new ActorsApi.UserConnected(user, Context.ConnectionId));
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
		var user = await _userAccessor.Current;
		_userSupervisor.ActorRef.Tell(new ActorsApi.UserDisconnected(user, Context.ConnectionId));
		if (Context.Items.TryGetValue(MentionsSubscriptionKey, out _)) {
			_userSupervisor.ActorRef.Tell(new ActorsApi.UnSubscribeOnMentions(user));
		}
		if (Context.Items.TryGetValue(LastMonitorSubscriptionKey, out _)) {
			_userSupervisor.ActorRef.Tell(new ActorsApi.UnSubscribeFromLastMonitor(user));
		}
		var identity = Context.User?.Identity;
		_logger.LogInformation("[{HubId}] User {Identifier} ({Name} IsAuthenticated = {IsAuthenticated}) disconnected",
			Context.Items.GetHashCode(),
			Context.UserIdentifier, identity?.Name, identity?.IsAuthenticated);
	}

	private string MentionsSubscriptionKey { get; set; } = "MentionsSubscription";
	private string LastMonitorSubscriptionKey { get; set; } = "LastMonitorSubscription";

	public async Task ReplyToNotification(int buildId, QuickReplyType quickReplyType, string comment) {
		await _mediator.Publish(new AddReplyCommentNotification {
			BuildConfigId = buildId,
			QuickReplyType = quickReplyType,
			Comment = comment
		});
	}

	public async Task SubscribeForMentions() {
		var user = await _userAccessor.Current;
		_userSupervisor.ActorRef.Tell(new ActorsApi.SubscribeToMentions(user, Clients.Caller));
		Context.Items[MentionsSubscriptionKey] = true;
	}

	public async Task<bool> SubscribeForLastMonitor() {
		var user = await _userAccessor.Current;
		_userSupervisor.ActorRef.Tell(new ActorsApi.SubscribeToLastMonitor(user));
		Context.Items[LastMonitorSubscriptionKey] = true;
		return true;
	}
}
