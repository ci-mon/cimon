using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AngleSharp.Html.Parser;

namespace Cimon.Data;

public class BuildComment
{
	public string Comment { get; set; }
	public string Author { get; set; }
	public IImmutableList<string> Mentions { get; set; } = ImmutableList<string>.Empty;
	public string Id { get; set; } = Guid.NewGuid().ToString("N");
}

public enum BuildDiscussionStatus
{
	Open, Closed
}
public record BuildDiscussionState
{
	public IImmutableList<BuildComment> Comments { get; set; } = ImmutableList<BuildComment>.Empty;
	public BuildDiscussionStatus Status { get; set; }
}

public class CommentData
{
	public string Author { get; set; } = "guest";
	public string Comment { get; set; } = string.Empty;
}

public class BuildDiscussionStoreService
{
	private readonly INotificationService _notificationService;
	private readonly BehaviorSubject<ImmutableList<BuildDiscussionService>> _allDiscussions =
		new(ImmutableList<BuildDiscussionService>.Empty);

	public BuildDiscussionStoreService(INotificationService notificationService) {
		_notificationService = notificationService;
	}

	public IObservable<BuildDiscussionService> GetDiscussionService(string buildId) {
		return _allDiscussions.SelectMany(b => b).Where(b => b.BuildId == buildId);
	}
	public async Task OpenDiscussion(string buildId) {
		var currentDiscussions = await _allDiscussions.FirstAsync();
		if (currentDiscussions.Any(x => x.BuildId == buildId)) {
			return;
		}
		var service = new BuildDiscussionService(buildId, _notificationService);
		_allDiscussions.OnNext(currentDiscussions.Add(service));
	}

	public async Task CloseDiscussion(string buildId) {
		var currentDiscussions = await _allDiscussions.FirstAsync();
		var exiting = currentDiscussions.Find(x => x.BuildId == buildId);
		if (exiting == null) {
			return;
		}
		await exiting.Close();
		_allDiscussions.OnNext(currentDiscussions.Remove(exiting));
	}
}

public class BuildDiscussionService
{

	private readonly INotificationService _notificationService;
	private readonly BehaviorSubject<BuildDiscussionState> _state = new(new BuildDiscussionState());
	public IObservable<BuildDiscussionState> State => _state;

	public BuildDiscussionService(string buildId, INotificationService notificationService) {
		BuildId = buildId;
		_notificationService = notificationService;
	}

	public string BuildId { get; }

	public async Task AddComment(CommentData data) {
		var comment = new BuildComment {
			Author = data.Author,
			Comment = data.Comment,
			Mentions = await ExtractMentionedUsers(data.Comment)
		};
		var currentState = await _state.FirstAsync();
		var state = currentState with {
			Comments = currentState.Comments.Add(comment)
		};
		await _notificationService.Notify(BuildId, comment.Id, data.Author, comment.Mentions);
		_state.OnNext(state);
	}
	
	private async Task<IImmutableList<string>> ExtractMentionedUsers(string content) {
		var parser = new HtmlParser();
		var document = await parser.ParseDocumentAsync(content);
		var mentionElements = document.QuerySelectorAll("span.mention");
		return mentionElements.Select(mention => mention.GetAttribute("data-id")).Where(x=>x != null).ToImmutableList()!;
	}

	public async Task Close() {
		_state.OnNext(new BuildDiscussionState {
			Status = BuildDiscussionStatus.Closed
		});
	}
}
