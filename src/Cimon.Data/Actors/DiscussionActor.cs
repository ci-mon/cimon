using System.Collections.Immutable;
using Akka.Actor;
using AngleSharp;
using AngleSharp.Html.Parser;
using Cimon.Contracts;
using Cimon.Contracts.CI;
using Cimon.Data.Discussions;
using Cimon.Data.Users;

namespace Cimon.Data.Actors;

public static class DiscussionActorApi
{
	internal record AddCommentMsg(CommentData CommentData);
	internal record UpdateCommentMsg(BuildComment Comment);
	internal record RemoveCommentMsg(BuildComment Comment);
	internal record ExecuteActionMsg(Guid ActionId);
	internal record Subscribe;
	internal record Unsubscribe;

	public static void AddComment(this ActorsApi.DiscussionHandle handle, CommentData commentData) =>
		handle.Discussion.Tell(new AddCommentMsg(commentData));
	public static void ExecuteAction(this ActorsApi.DiscussionHandle handle, Guid id) =>
		handle.Discussion.Tell(new ExecuteActionMsg(id));
	public static void UpdateComment(this ActorsApi.DiscussionHandle handle, BuildComment buildComment) =>
		handle.Discussion.Tell(new UpdateCommentMsg(buildComment));
	public static void RemoveComment(this ActorsApi.DiscussionHandle handle, BuildComment buildComment) =>
		handle.Discussion.Tell(new RemoveCommentMsg(buildComment));
}

public class DiscussionActor : ReceiveActor
{
	private readonly INotificationService _notificationService;
	private BuildDiscussionState _state = new();
	private int _buildConfigId;
	private readonly Dictionary<Guid, BuildInfoActionDescriptor> _actions = new();
	private readonly ITechnicalUsers _technicalUsers;
	private readonly List<IActorRef> _subscribers = new();

	public DiscussionActor(INotificationService notificationService, ITechnicalUsers technicalUsers) {
		_notificationService = notificationService;
		_technicalUsers = technicalUsers;
		ReceiveAsync<BuildInfo>(async info => {
			if (_state.Status == BuildDiscussionStatus.Unknown) {
				var state = _state with {
					Status = BuildDiscussionStatus.Open
				};
				_buildConfigId = info.BuildConfigId;
				await OpenDiscussion(info);
				StateHasChanged(state);
			}
		});
		ReceiveAsync<DiscussionActorApi.AddCommentMsg>(msg => AddComment(msg.CommentData));
		Receive<DiscussionActorApi.UpdateCommentMsg>(msg => UpdateComment(msg.Comment));
		Receive<DiscussionActorApi.RemoveCommentMsg>(msg => RemoveComment(msg.Comment));
		ReceiveAsync<PoisonPill>(_ => CloseDiscussion());
		ReceiveAsync<DiscussionActorApi.ExecuteActionMsg>(msg => ExecuteAction(msg.ActionId));
		Receive<DiscussionActorApi.Subscribe>(_ => _subscribers.Add(Sender));
		Receive<DiscussionActorApi.Unsubscribe>(_ => _subscribers.Remove(Sender));
	}

	private async Task AddComment(CommentData data) {
		var comment = new BuildComment {
			Author = data.Author,
			Comment = data.Comment,
			Mentions = await ExtractMentions(data.Comment)
		};
		var state = _state with {
			Comments = _state.Comments.Add(comment)
		};
		var commentSimpleText = ExtractText(comment);
		StateHasChanged(state);
		// TODO _buildConfigId
		await _notificationService.Notify(_buildConfigId.ToString(), comment.Id, data.Author, comment.Mentions, commentSimpleText);
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
			.Select(x => new MentionedEntityId(x.id!, x.value!, x.type))
			.ToImmutableList();
	}

	private void StateHasChanged(BuildDiscussionState state) {
		_state = state;
		Context.Parent.Tell(state);
		foreach (var actorRef in _subscribers) {
			actorRef.Tell(state);
		}
	}

	private void RemoveComment(BuildComment comment) {
		var state = _state with {
			Comments = _state.Comments.Remove(comment)
		};
		StateHasChanged(state);
	}

	private void UpdateComment(BuildComment comment) {
		comment.ModifiedOn = DateTime.UtcNow;
		var state = _state with {
			Comments = _state.Comments.Replace(comment, comment)
		};
		StateHasChanged(state);
	}

	private void RegisterActions(IReadOnlyCollection<BuildInfoActionDescriptor> actions) {
		foreach (var item in actions) {
			_actions[item.Id] = item;
		}
	}

	private async Task ExecuteAction(Guid id) {
		if (_actions.Remove(id, out var action)) {
			await action.Execute();
		}
	}

	private string GetUserMention(string userId, string userName) {
		return
			$"""<span class="mention" data-index="1" data-denotation-char="@" data-id="{userId}" data-value="{userName}"><span contenteditable="false"><span class="ql-mention-denotation-char">@</span>{userName}</span></span> """;
	}
	private string BuildCommentMessage(BuildInfo buildInfo) {
		var users = buildInfo.Changes.Select(x=>x.Author);
		var values = users?.Select(u => GetUserMention(u.Name, u.FullName)) ?? new[] { "somebody" };
		return $"<p>Build failed by: {string.Join(", ", values)}</p>";
	}
	
	private async Task OpenDiscussion(BuildInfo buildInfo) {
		await AddComment(new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = BuildCommentMessage(buildInfo)
		});
		if (buildInfo is IBuildInfoActionsProvider actionProvider) {
			var actions = actionProvider.GetAvailableActions();
			RegisterActions(actions);
			foreach (var group in actions.GroupBy(x=>x.GroupDescription)) {
				var innerActions = string.Join(",", group.Select(x => $"{x.Description}[{x.Id}]"));
				await AddComment(new CommentData {
					Author = _technicalUsers.MonitoringBot,
					Comment = $"{group.Key} {innerActions}"
				});
			}
		}
	}

	private async Task CloseDiscussion() {
		var comments = _state.Comments;
		var mentionedUsers = comments.SelectMany(c => c.Mentions.Where(x => x.Type == MentionedEntityType.User))
			.ToList();
		if (!mentionedUsers.Any()) return;
		var values = mentionedUsers.DistinctBy(x => x.Name).Select(u => GetUserMention(u.Name, u.DisplayName));
		await AddComment(new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = $"<p>{string.Join(", ", values)} build is green now.</p>"
		});
	}
}
