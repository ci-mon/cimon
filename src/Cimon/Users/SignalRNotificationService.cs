using Cimon.Contracts;
using Cimon.Data.Users;
using Cimon.NativeApp;
using MediatR;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Users;

public class SignalRNotificationService : INotificationService, INotificationHandler<NativeAppPublished>
{
	private readonly IHubContext<UserHub, IUserClientApi> _hubContext;

	public SignalRNotificationService(IHubContext<UserHub, IUserClientApi> hubContext) {
		_hubContext = hubContext;
	}

	public async Task Notify(string buildId, string commentId, string messageAuthor, 
			IReadOnlyCollection<MentionedEntityId> groups, string comment) {
		await _hubContext.Clients.Groups(groups.Select(x=>x.Name)).NotifyWithUrl(buildId, $"/buildDiscussion/{buildId}#{commentId}",
			$"{messageAuthor} mentioned you in a comment", comment);
	}

	public async Task Handle(NativeAppPublished notification, CancellationToken cancellationToken) {
		await _hubContext.Clients.All.CheckForUpdates();
	}
}
