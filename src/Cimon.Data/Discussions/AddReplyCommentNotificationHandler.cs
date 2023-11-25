using Cimon.Data.Common;
using Cimon.Data.Users;
using MediatR;

namespace Cimon.Data.Discussions;

class AddReplyCommentNotificationHandler(ICurrentUserAccessor currentUserAccessor)
	: INotificationHandler<AddReplyCommentNotification>
{
	public async Task Handle(AddReplyCommentNotification notification, CancellationToken cancellationToken) {
		var comment = notification.QuickReplyType switch {
			QuickReplyType.Wip => "I am working on it",
			QuickReplyType.RequestingRollback => "Could you rollback my changes?",
			QuickReplyType.RequestingMute => "Could you mute and assign investigation?",
			_ => notification.Comment
		};
		var handle =
			await AppActors.Instance.DiscussionsService.Ask(new ActorsApi.FindDiscussion(notification.BuildConfigId));
		handle.AddComment(new CommentData {
			Author = await currentUserAccessor.Current,
			Comment = comment
		});
	}
}