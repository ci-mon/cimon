using System.Collections.Immutable;
using System.Reactive.Linq;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;

namespace Cimon.Data;

public static class ActorsApi
{
	public record WatchMonitor(string Id) : IMessageWithResponse<IObservable<MonitorData>>;

	public abstract record DiscussionAction(int BuildConfigId);

	public record OpenDiscussion(int BuildConfigId, BuildInfo BuildInfo) : DiscussionAction(BuildConfigId);

	public record CloseDiscussion(int BuildConfigId) : DiscussionAction(BuildConfigId);

	public record DiscussionHandle(bool Active, IActorRef Discussion, IObservable<BuildDiscussionState> State)
	{
		public static DiscussionHandle Empty { get; } = new(false, ActorRefs.Nobody,
			Observable.Empty<BuildDiscussionState>());
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
}
