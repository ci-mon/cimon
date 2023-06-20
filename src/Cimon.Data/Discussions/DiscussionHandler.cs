using System.Reactive.Linq;
using Cimon.Contracts;
using Cimon.Data.Users;
using MediatR;

namespace Cimon.Data.Discussions;

class DiscussionHandler: INotificationHandler<DiscussionOpenNotification>, INotificationHandler<DiscussionClosedNotification>
{
	private readonly ITechnicalUsers _technicalUsers;
	public DiscussionHandler(ITechnicalUsers technicalUsers) {
		_technicalUsers = technicalUsers;
	}

	private string BuildCommentMessage(BuildInfo buildInfo) {
		var users = buildInfo.CommitterUsers;
		var values = users?.Select(u => GetUserMention(u.Name, u.FullName)) ?? new[] { "somebody" };
		return $"<p>Build failed by: {string.Join(", ", values)}</p>";
	}

	private string GetUserMention(string userId, string userName) {
		return
			$"""<span class="mention" data-index="1" data-denotation-char="@" data-id="{userId}" data-value="{userName}"><span contenteditable="false"><span class="ql-mention-denotation-char">@</span>{userName}</span></span> """;
	}
	public async Task Handle(DiscussionOpenNotification notification, CancellationToken cancellationToken) {
		await notification.Discussion.AddComment(new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = BuildCommentMessage(notification.BuildInfo)
		});
		if (notification.BuildInfo is IBuildInfoActionsProvider actionProvider) {
			var actions = actionProvider.GetAvailableActions();
			notification.Discussion.RegisterActions(actions);
			foreach (var group in actions.GroupBy(x=>x.GroupDescription)) {
				var innerActions = string.Join(",", group.Select(x => $"{x.Description}[{x.Id}]"));
				await notification.Discussion.AddComment(new CommentData {
					Author = _technicalUsers.MonitoringBot,
					Comment = $"{group.Key} {innerActions}"
				});
			}
		}
	}

	public async Task Handle(DiscussionClosedNotification notification, CancellationToken cancellationToken) {
		var discussion = notification.Discussion;
		var comments = await discussion.State.Select(x => x.Comments).FirstAsync();
		var mentionedUsers = comments.SelectMany(c => c.Mentions.Where(x => x.Type == MentionedEntityType.User)).ToList();
		if (!mentionedUsers.Any()) return;
		var values = mentionedUsers.DistinctBy(x => x.Name).Select(u => GetUserMention(u.Name, u.DisplayName));
		await discussion.AddComment(new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = $"<p>{string.Join(", ", values)} build is green now.</p>"
		});
	}
}