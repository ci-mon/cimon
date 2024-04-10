using System.Collections.Immutable;
using System.Reactive.Linq;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;
using Cimon.Data.Users;
using Cimon.DB.Models;
using Optional.Collections;
using User = Cimon.Contracts.User;

namespace Cimon.Data;

public static class ActorsApi
{
	public abstract record MonitorMessage(string Id);
	public record WatchMonitor(string Id) : MonitorMessage(Id), IMessageWithResponse<IObservable<MonitorData>>;
	public record RefreshMonitor(string Id) : MonitorMessage(Id);
	public record ReorderMonitorItems(string Id, BuildConfigModel Target, BuildConfigModel PlaceBefore) : MonitorMessage(Id);
	public record UpdateViewSettings(string Id, ViewSettings viewSettings) : MonitorMessage(Id);

	public record WatchMonitorByActor(string Id) : MonitorMessage(Id);
	public record UnWatchMonitorByActor(string Id) : MonitorMessage(Id);
	public record MonitorInfo(MonitorModel MonitorModel, IEnumerable<IBuildInfoSnapshot> BuildInfos);

	public record GetActiveUserNames(): IMessageWithResponse<IObservable<IImmutableSet<string>>>;
	public abstract record DiscussionAction(int BuildConfigId);

	public record OpenDiscussion(BuildConfig BuildConfig, BuildInfo BuildInfo) : DiscussionAction(BuildConfig.Id);

	public record CloseDiscussion(int BuildConfigId) : DiscussionAction(BuildConfigId);

	public record DiscussionHandle(bool Active, IActorRef Discussion, IObservable<BuildDiscussionState> State,
		IObservable<IEnumerable<IDiscussionBuildData>> Builds)
	{
		public static DiscussionHandle Empty { get; } = new(false, ActorRefs.Nobody,
			Observable.Empty<BuildDiscussionState>(), 
			Observable.Empty<ImmutableList<IDiscussionBuildData>>());
	}

	public record FindDiscussion(int BuildConfigId)
		: DiscussionAction(BuildConfigId), IMessageWithResponse<DiscussionHandle>;
	
	public record UserMessage<TMessage>(string UserName, TMessage Payload) : UserMessage(UserName)
	{
		public override object Message => Payload;
	}

	public abstract record UserMessage(string UserName)
	{
		public abstract object Message { get; }
	}
	public record GetMentions;
	public record GetUserMentions(string UserName) : UserMessage<GetMentions>(UserName, new GetMentions()),
		IMessageWithResponse<IObservable<IImmutableList<MentionInfo>>>;
	public record SubscribeToMentions(User User, IUserClientApi Caller) : UserMessage<User>(User.Name.Name, User);
	public record UnSubscribeOnMentions(User User) : UserMessage<User>(User.Name.Name, User);
	public record SubscribeToMonitor(User User, string? MonitorId) : UserMessage<User>(User.Name.Name, User);
	public record UnSubscribeFromMonitor(User User, string? MonitorId) : UserMessage<User>(User.Name.Name, User);
	public record UpdateLastMonitor(User User, string? MonitorId) : UserMessage<User>(User.Name.Name, User);

	public static async Task<IObservable<IReadOnlyCollection<MentionInBuildConfig>>> GetMentionsWithBuildConfig(
			this BuildConfigService buildConfigService, User user) {
		var mentionsObservable = await AppActors.GetMentions(user);
		return buildConfigService.GetMentionsWithBuildConfig(mentionsObservable);
	}

	public static IObservable<IReadOnlyCollection<MentionInBuildConfig>> GetMentionsWithBuildConfig(this BuildConfigService buildConfigService,
			IObservable<IImmutableList<MentionInfo>> mentionsObservable) {
		return mentionsObservable.CombineLatest(buildConfigService.BuildConfigs, (mentions, configs) => {
			return mentions.Select(m => new MentionInBuildConfig(m, configs.FirstOrNone(c => c.Id == m.BuildConfigId)))
				.ToImmutableList();
		});
	}

	public record BuildInfoItem(BuildInfo BuildInfo, int BuildConfigId);

	public record UserConnected(User User, string ConnectionId);
	public record UserDisconnected(User User, string ConnectionId);
}
