using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using AngleSharp;
using AngleSharp.Html.Parser;
using Cimon.Contracts;
using Cimon.Data.Users;

namespace Cimon.Data.Discussions;

public class BuildDiscussionService : IBuildDiscussionService
{

	private readonly INotificationService _notificationService;
	private readonly BehaviorSubject<BuildDiscussionState> _state = new(new BuildDiscussionState {
		Status = BuildDiscussionStatus.Open
	});
	public IObservable<BuildDiscussionState> State => _state;

	public IObservable<IImmutableList<BuildComment>> Comments => _state.Select(x =>
		x.Status == BuildDiscussionStatus.Open ? x.Comments : ImmutableArray<BuildComment>.Empty);

	public BuildDiscussionService(string buildId, INotificationService notificationService) {
		BuildId = buildId;
		_notificationService = notificationService;
	}

	public string BuildId { get; }

	public async Task AddComment(CommentData data) {
		var comment = new BuildComment {
			Author = data.Author,
			Comment = data.Comment,
			Mentions = await ExtractMentions(data.Comment)
		};
		var currentState = await _state.FirstAsync();
		var state = currentState with {
			Comments = currentState.Comments.Add(comment)
		};
		var commentSimpleText = ExtractText(comment);
		await _notificationService.Notify(BuildId, comment.Id, data.Author.Name, comment.Mentions, commentSimpleText);
		_state.OnNext(state);
	}

	private string ExtractText(BuildComment comment) {
		var context = BrowsingContext.New(Configuration.Default);
		var parser = context.GetService<IHtmlParser>() ?? throw new Exception("Can't get parser");
		var document = parser.ParseDocument(comment.Comment);
		return document.DocumentElement.TextContent;
	}

	private async Task<IImmutableList<MentionedEntityId>> ExtractMentions(string content) {
		var parser = new HtmlParser();
		var document = await parser.ParseDocumentAsync(content);
		var mentionElements = document.QuerySelectorAll("span.mention");
		return mentionElements
			.Select(mention => new {
				id = mention.GetAttribute("data-id"),
				type = mention.GetAttribute("data-denotation-char") == "#" ? MentionedEntityType.Team : MentionedEntityType.User,
				value = mention.GetAttribute("data-value")
			})
			.Where(x => x.id is not null)
			.Select(x => new MentionedEntityId(x.id!, x.type))
			.ToImmutableList();
	}

	public async Task Close() {
		var currentState = await _state.FirstAsync();
		_state.OnNext(currentState with {
			Status = BuildDiscussionStatus.Closed
		});
		_state.OnCompleted();
	}

	public async Task RemoveComment(BuildComment comment) {
		var currentState = await _state.FirstAsync();
		var state = currentState with {
			Comments = currentState.Comments.Remove(comment)
		};
		_state.OnNext(state);
	}

	public async Task UpdateComment(BuildComment comment) {
		comment.ModifiedOn = DateTime.UtcNow;
		var currentState = await _state.FirstAsync();
		var state = currentState with {
			Comments = currentState.Comments.Replace(comment, comment)
		};
		_state.OnNext(state);
	}
}
