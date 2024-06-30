using Cimon.Contracts;

namespace Cimon.Data.Users;

public interface INotificationService
{
	public Task Notify(int buildConfigId, string commentId, User messageAuthor, IReadOnlyCollection<MentionedEntityId> groups,
		string comment);
	public Task HideNotification(int buildConfigId, string commentId, IReadOnlyCollection<MentionedEntityId> groups);
}
