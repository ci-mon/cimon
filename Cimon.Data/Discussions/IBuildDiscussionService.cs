namespace Cimon.Data;

public interface IBuildDiscussionService
{
	IObservable<BuildDiscussionState> State { get; }
	string BuildId { get; }
	Task AddComment(CommentData data);
	Task Close();
	Task RemoveComment(BuildComment comment);
	Task UpdateComment(BuildComment comment);
}