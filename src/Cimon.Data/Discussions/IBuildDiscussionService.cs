namespace Cimon.Data.Discussions;

using Cimon.Contracts.CI;

public interface IBuildDiscussionService
{
	IObservable<BuildDiscussionState> State { get; }
	string BuildId { get; }
	Task AddComment(CommentData data);
	Task Close();
	Task RemoveComment(BuildComment comment);
	Task UpdateComment(BuildComment comment);
	void RegisterActions(IReadOnlyCollection<BuildInfoActionDescriptor> actions);
	Task ExecuteAction(Guid id);
}