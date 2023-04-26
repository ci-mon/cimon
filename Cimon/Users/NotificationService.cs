using Cimon.Data;
using Cimon.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

public class NotificationService : INotificationService
{
	private readonly IHubContext<UserHub, IUserClientApi> _hubContext;

	public NotificationService(IHubContext<UserHub, IUserClientApi> hubContext) {
		_hubContext = hubContext;
	}

	public async Task Notify(string buildId, string commentId, string messageAuthor, IReadOnlyCollection<string> groups) {
		await _hubContext.Clients.Groups(groups).NotifyWithUrl($"/buildStatus/{buildId}#{commentId}",
			$"Hi there {messageAuthor} mentioned you in a comment");
	}
}
