using Cimon.Contracts;

namespace Cimon.Data.Users;

public interface INotificationService
{
	public Task Notify(string buildId, string commentId, string messageAuthor, IReadOnlyCollection<MentionedEntityId> groups,
		string comment);
}
