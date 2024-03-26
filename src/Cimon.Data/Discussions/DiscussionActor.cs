using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Routing;
using AngleSharp;
using AngleSharp.Html.Parser;
using Cimon.Contracts;
using Cimon.Contracts.CI;
using Cimon.Data.BuildInformation;
using Cimon.Data.Users;

namespace Cimon.Data.Discussions;

enum ChangeType
{
	Add, Remove
}

record BuildCommentChange(ChangeType ChangeType, BuildComment Comment, int BuildConfigId);

public class DiscussionActor : ReceiveActor
{
	private readonly INotificationService _notificationService;
	private BuildDiscussionState _state = new();
	private BuildConfig _buildConfig;
	private readonly Dictionary<Guid, BuildInfoActionDescriptor> _actions = new();
	private readonly ITechnicalUsers _technicalUsers;
	private readonly IActorRef _stateSubscribers;
	private IActorRef _commentsSubscribers = ActorRefs.Nobody;
	private DiscussionData _discussionData; 

	public DiscussionActor(INotificationService notificationService, ITechnicalUsers technicalUsers) {
		_notificationService = notificationService;
		_technicalUsers = technicalUsers;
		_stateSubscribers = Context.ActorOf(Props.Empty.WithRouter(new BroadcastGroup()));
		ReceiveAsync<BuildConfig>(OnBuildConfig);
		Receive<DiscussionData>(state => _discussionData = state);
		ReceiveAsync<ActorsApi.BuildInfoItem>(async msg => {
			var buildData = await _discussionData!.Builds.Timeout(TimeSpan.FromSeconds(5)).SelectMany(x => x)
				.Where(x => x.BuildConfig.Id == msg.BuildConfigId).FirstOrDefaultAsync();
			buildData?.BuildInfo.OnNext(msg.BuildInfo);
			if (_state.Status == BuildDiscussionStatus.Unknown) {
				BuildDiscussionState state = _state with {
					Status = BuildDiscussionStatus.Open
				};
				StateHasChanged(state);
				await OpenDiscussion(msg.BuildInfo);
			}
		});
		ReceiveAsync<DiscussionActorApi.AddCommentMsg>(msg => AddComment(msg.CommentData));
		ReceiveAsync<DiscussionActorApi.UpdateCommentMsg>(msg => UpdateComment(msg.Comment));
		Receive<DiscussionActorApi.RemoveCommentMsg>(msg => RemoveComment(msg.Comment));
		ReceiveAsync<PoisonPill>(_ => CloseDiscussion());
		ReceiveAsync<DiscussionActorApi.ExecuteActionMsg>(msg => ExecuteAction(msg.ActionId));
		Receive<DiscussionActorApi.SubscribeForState>(_ => {
			Context.Watch(Sender);
			_stateSubscribers.Tell(new AddRoutee(new ActorRefRoutee(Sender)));
			if (_state.Status != BuildDiscussionStatus.Unknown) {
				Sender.Tell(_state);
			}
		});
		Receive<Terminated>(terminated =>
			_stateSubscribers.Tell(new RemoveRoutee(new ActorRefRoutee(terminated.ActorRef))));
		Receive<DiscussionActorApi.UnsubscribeForState>(_ =>
			_stateSubscribers.Tell(new RemoveRoutee(new ActorRefRoutee(Sender))));
		Receive<DiscussionActorApi.SubscribeForComments>(_ => {
			if (_commentsSubscribers.Equals(ActorRefs.Nobody)) {
				_commentsSubscribers = Context.ActorOf(Props.Empty.WithRouter(new BroadcastGroup()));
			}
			_commentsSubscribers.Tell(new AddRoutee(new ActorRefRoutee(Sender)));
			foreach (var comment in _state.Comments) {
				Sender.Tell(new BuildCommentChange(ChangeType.Add, comment, _buildConfig!.Id));
			}
		});
		Receive<DiscussionActorApi.UnsubscribeForComments>(_ =>
			_commentsSubscribers.Tell(new RemoveRoutee(new ActorRefRoutee(Sender))));
	}

	private async Task OnBuildConfig(BuildConfig buildConfig) {
		_buildConfig = buildConfig;
		AppActors.Instance.BuildInfoService.Tell(new BuildInfoServiceActorApi.Subscribe(buildConfig));
		_state = _state with { Status = BuildDiscussionStatus.Unknown };
		var items = await _discussionData.Builds.Timeout(TimeSpan.FromSeconds(5)).FirstOrDefaultAsync() ??
			ImmutableList.Create<DiscussionBuildData>();
		var buildData = items.FirstOrDefault(x => x.BuildConfig.Id == buildConfig.Id);
		if (buildData is not null) {
			buildData = buildData with { BuildConfig = buildConfig };
		} else {
			buildData = new DiscussionBuildData(buildConfig, new ReplaySubject<BuildInfo>(1));
		}
		items = items.RemoveAll(x => x.BuildConfig.Id == buildConfig.Id).Add(buildData);
		_discussionData.Builds.OnNext(items);
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
		_commentsSubscribers.Tell(new BuildCommentChange(ChangeType.Add, comment, _buildConfig!.Id));
		var commentSimpleText = ExtractText(comment);
		StateHasChanged(state);
		await _notificationService.Notify(_buildConfig.Id, comment.Id, data.Author, comment.Mentions,
			commentSimpleText);
	}
	
	private string ExtractText(BuildComment comment) {
		var context = BrowsingContext.New(Configuration.Default);
		var parser = context.GetService<IHtmlParser>() ?? throw new InvalidOperationException("Can't get parser");
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
		_stateSubscribers.Tell(state);
		_discussionData.Subject.OnNext(state);
	}

	private void RemoveComment(BuildComment comment) {
		var state = _state with {
			Comments = _state.Comments.Remove(comment)
		};
		_commentsSubscribers.Tell(new BuildCommentChange(ChangeType.Remove, comment, _buildConfig!.Id));
		StateHasChanged(state);
	}

	private async Task UpdateComment(BuildComment comment) {
		var stateComments = _state.Comments;
		var oldComment = stateComments.FirstOrDefault(x => x.Id == comment.Id);
		if (oldComment is not null) {
			_commentsSubscribers.Tell(new BuildCommentChange(ChangeType.Remove, oldComment, _buildConfig!.Id));
			stateComments = stateComments.Remove(oldComment);
		}
		comment.ModifiedOn = DateTime.UtcNow;
		comment.Mentions = await ExtractMentions(comment.Comment);
		var state = _state with {
			Comments = stateComments.Add(comment)
		};
		_commentsSubscribers.Tell(new BuildCommentChange(ChangeType.Add, comment, _buildConfig!.Id));
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
		var users = buildInfo.Changes.Select(x=>x.Author).Distinct();
		var values = users.Select(u => GetUserMention(u.Name, u.FullName)).ToArray();
		var message = values.Any() ? $"Build failed by: {string.Join(", ", values)}" : "Who failed the build?";
		return $"<p>{message}</p>";
	}
	
	private async Task OpenDiscussion(BuildInfo buildInfo) {
		var commentData = new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = BuildCommentMessage(buildInfo)
		};
		await AddComment(commentData);
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
		var mentionedUsers = comments
			.SelectMany(c => c.Mentions.Where(x => x.Type == MentionedEntityType.User))
			.ToList();
		if (!mentionedUsers.Any()) return;
		var values = mentionedUsers.DistinctBy(x => x.Name)
			.Select(u => GetUserMention(u.Name, u.DisplayName));
		await AddComment(new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = $"<p>{string.Join(", ", values)} build is green now.</p>"
		});
	}

	protected override void PostStop() {
		_discussionData?.Subject.OnCompleted();
		_discussionData?.Subject.Dispose();
	}
}
