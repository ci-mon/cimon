using Cimon.Contracts;
using Cimon.Data.Users;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

public class NotificationService : INotificationService
{
	private readonly IHubContext<UserHub, IUserClientApi> _hubContext;

	public NotificationService(IHubContext<UserHub, IUserClientApi> hubContext) {
		_hubContext = hubContext;
	}

	public async Task Notify(string buildId, string commentId, string messageAuthor, 
			IReadOnlyCollection<MentionedEntityId> groups, string comment) {
		await _hubContext.Clients.Groups(groups.Select(x=>x.Name)).NotifyWithUrl(buildId, $"/buildDiscussion/{buildId}#{commentId}",
			$"{messageAuthor} mentioned you in a comment", comment);
	}
}
