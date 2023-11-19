using System.Reactive.Linq;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Discussions;

namespace Cimon.Data.Actors;

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
}
