namespace Cimon.Data;

public interface INotificationService
{
	public Task Notify(string buildId, string commentId, string messageAuthor, IReadOnlyCollection<string> groups,
		string comment);
}
