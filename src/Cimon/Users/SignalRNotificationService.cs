﻿using Cimon.Contracts;
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

	public async Task Notify(int buildConfigId, string commentId, User messageAuthor,
		IReadOnlyCollection<MentionedEntityId> groups, string comment) {
		var groupNames = groups.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x));
		await _hubContext.Clients.Groups(groupNames).NotifyWithUrl(buildConfigId, commentId,
			$"/buildDiscussion/{buildConfigId}#{commentId}", $"{messageAuthor.Name} mentioned you in a comment",
			comment, messageAuthor.Email ?? string.Empty);
	}

	public async Task HideNotification(int buildConfigId, string commentId,
		IReadOnlyCollection<MentionedEntityId> groups) {
		var groupNames = groups.Select(x => x.Name).Where(x => !string.IsNullOrWhiteSpace(x));
		await _hubContext.Clients.Groups(groupNames).RemoveNotification(buildConfigId, commentId);
	}

	public async Task Handle(NativeAppPublished notification, CancellationToken cancellationToken) {
		await _hubContext.Clients.All.CheckForUpdates();
	}
}
