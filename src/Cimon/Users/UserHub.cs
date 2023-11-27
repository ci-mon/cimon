using System.Reactive.Linq;
using Cimon.Data;
using Cimon.Data.CIConnectors;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;
using Cimon.Data.Users;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

public record ExtendedMentionInfo(int BuildConfigId, int CommentsCount, string BuildConfigKey) : MentionInfo(BuildConfigId, CommentsCount);

public interface IUserClientApi
{
	Task NotifyWithUrl(int buildConfigId, string url, string header, string message, string authorEmail);
	Task UpdateMentions(IEnumerable<ExtendedMentionInfo> mentions);
	Task CheckForUpdates();
}

[Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme}")]
public class UserHub : Hub<IUserClientApi>
{
	private readonly ILogger _logger;
	private readonly IMediator _mediator;
	private readonly UserManager _userManager;
	private readonly ICurrentUserAccessor _userAccessor;
	private readonly IHubContext<UserHub, IUserClientApi> _hubContext;
	private const string MentionsSubscriptionKey = "MentionsSubscription";
	private readonly BuildConfigService _buildConfigService;

	public UserHub(ILogger<UserHub> logger, IMediator mediator, UserManager userManager, 
			ICurrentUserAccessor userAccessor, 
			IHubContext<UserHub, IUserClientApi> hubContext, BuildConfigService buildConfigService) {
		_logger = logger;
		_mediator = mediator;
		_userManager = userManager;
		_userAccessor = userAccessor;
		_hubContext = hubContext;
		_buildConfigService = buildConfigService;
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
		var mentions = await _buildConfigService.GetMentionsWithBuildConfig(user);
		
		subscription = mentions
			.Select(m => _hubContext.Clients.Client(connectionId).UpdateMentions(
				m.Select(x=> 
					new ExtendedMentionInfo(x.Mention.BuildConfigId, x.Mention.CommentsCount, x.BuildConfig.Map(c=>c.Key).ValueOr(string.Empty)))))
			.Subscribe();
		Context.Items[MentionsSubscriptionKey] = subscription;
	}

	public async Task SubscribeForLastMonitor() {
		
	}

}

