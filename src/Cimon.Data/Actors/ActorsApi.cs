using System.Reactive.Linq;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Discussions;

namespace Cimon.Data.Actors;

public static class ActorsApi
{
	public record WatchMonitor(string Id) : IMessageWithResponse<IObservable<MonitorData>>;

	public abstract record DiscussionAction(string BuildConfigId);

	public record OpenDiscussion(string BuildConfigId, BuildInfo BuildInfo) : DiscussionAction(BuildConfigId);

	public record CloseDiscussion(string BuildConfigId) : DiscussionAction(BuildConfigId);

	public record DiscussionHandle(bool Active, IActorRef Api, IObservable<BuildDiscussionState> State)
	{
		public static DiscussionHandle Empty { get; } = new(false, ActorRefs.Nobody,
			Observable.Empty<BuildDiscussionState>());
	}

	public record FindDiscussion(string BuildConfigId)
		: DiscussionAction(BuildConfigId), IMessageWithResponse<DiscussionHandle>;
}
