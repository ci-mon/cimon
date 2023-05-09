using Cimon.Data.Users;

namespace Cimon.Data;

public interface INotificationService
{
	public Task Notify(string buildId, string commentId, string messageAuthor, IReadOnlyCollection<UserId> groups,
		string comment);
}
