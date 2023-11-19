using System.Reactive.Linq;
using Cimon.Data.Discussions;
using Cimon.Data.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

public interface IUserClientApi
{
	Task NotifyWithUrl(string buildConfigId, string url, string header, string message, string authorEmail);
	Task UpdateMentions(IEnumerable<MentionInfo> mentions);
	Task CheckForUpdates();
}

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
public class UserHub : Hub<IUserClientApi>
{
	private readonly ILogger _logger;
	private readonly IMediator _mediator;
	private readonly UserManager _userManager;
	private readonly ICurrentUserAccessor _userAccessor;
	private readonly MentionsService _mentionsService;
	private readonly IHubContext<UserHub, IUserClientApi> _hubContext;
	private const string MentionsSubscriptionKey = "MentionsSubscription";

	public UserHub(ILogger<UserHub> logger, IMediator mediator, UserManager userManager, 
			ICurrentUserAccessor userAccessor, MentionsService mentionsService, 
			IHubContext<UserHub, IUserClientApi> hubContext) {
		_logger = logger;
		_mediator = mediator;
		_userManager = userManager;
		_userAccessor = userAccessor;
		_mentionsService = mentionsService;
		_hubContext = hubContext;
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
		if (Context.Items.TryGetValue(MentionsSubscriptionKey, out var subscription)) {
			(subscription as IDisposable)?.Dispose();
		}
		var identity = Context.User?.Identity;
		_logger.LogInformation("[{HubId}] User {Identifier} ({Name} IsAuthenticated = {IsAuthenticated}) disconnected",
			Context.Items.GetHashCode(),
			Context.UserIdentifier, identity?.Name, identity?.IsAuthenticated);
	}

	public async Task ReplyToNotification(string buildId, QuickReplyType quickReplyType, string comment) {
		await _mediator.Publish(new AddReplyCommentNotification {
			BuildConfigId = int.Parse(buildId),// TODO change type
			QuickReplyType = quickReplyType,
			Comment = comment
		});
	}

	public async Task SubscribeForMentions() {
		if (Context.Items.TryGetValue(MentionsSubscriptionKey, out var subscription)) {
			return;
		}
		var user = await _userAccessor.Current;
		var connectionId = Context.ConnectionId;
		subscription = _mentionsService.GetMentions(user)
			.Select(m => _hubContext.Clients.Client(connectionId).UpdateMentions(m))
			.Subscribe();
		Context.Items[MentionsSubscriptionKey] = subscription;
	}

	public async Task SubscribeForLastMonitor() {
		
	}

}

