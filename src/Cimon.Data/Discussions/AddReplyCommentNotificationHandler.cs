using System.Reactive.Linq;
using Cimon.Data.Users;
using MediatR;

namespace Cimon.Data.Discussions;

class AddReplyCommentNotificationHandler : INotificationHandler<AddReplyCommentNotification>
{
	private readonly BuildDiscussionStoreService _discussionStoreService;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	public AddReplyCommentNotificationHandler(BuildDiscussionStoreService discussionStoreService, ICurrentUserAccessor currentUserAccessor) {
		_discussionStoreService = discussionStoreService;
		_currentUserAccessor = currentUserAccessor;
	}

	public async Task Handle(AddReplyCommentNotification notification, CancellationToken cancellationToken) {
		var disc = await _discussionStoreService.GetDiscussionService(notification.BuildId)
			.Timeout(TimeSpan.FromSeconds(2)).FirstAsync();
		var comment = notification.QuickReplyType switch {
			QuickReplyType.Wip => "I am working on it",
			QuickReplyType.RequestingRollback => "Could you rollback my changes?",
			QuickReplyType.RequestingMute => "Could you mute and assign investigation?",
			_ => notification.Comment ?? "???",
		};
		await disc.AddComment(new CommentData {
			Author = await _currentUserAccessor.Current,
			Comment = comment
		});
	}
}