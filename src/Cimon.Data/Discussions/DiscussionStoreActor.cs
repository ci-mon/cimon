using System.Collections.Immutable;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Common;

namespace Cimon.Data.Discussions;

using Cimon.Contracts.CI;

public record DiscussionBuildData(BuildConfig BuildConfig, ReplaySubject<BuildInfo> BuildInfo) : IDiscussionBuildData
{
	IObservable<BuildInfo> IDiscussionBuildData.BuildInfo => BuildInfo;
}

public interface IDiscussionBuildData {
	BuildConfig BuildConfig { get; }
	IObservable<BuildInfo> BuildInfo { get; }
}

public record DiscussionData(IActorRef Child, ReplaySubject<BuildDiscussionState> Subject,
	ReplaySubject<IImmutableList<DiscussionBuildData>> Builds);

public class DiscussionStoreActor : ReceiveActor
{
	private readonly IActorRef _mentionsMonitor;
	private readonly Dictionary<int, DiscussionData> _discussions = new();
	public DiscussionStoreActor(IActorRef mentionsMonitor) {
		_mentionsMonitor = mentionsMonitor;
		Receive<ActorsApi.FindDiscussion>(FindDiscussion);
		Receive<ActorsApi.CloseDiscussion>(CloseDiscussion);
		Receive<ActorsApi.OpenDiscussion>(OpenDiscussion);
	}

	private void OpenDiscussion(ActorsApi.OpenDiscussion req) {
		if (_discussions.TryGetValue(req.BuildConfigId, out var value)) {
			value.Child.Forward(new DiscussionActorApi.SubscribeForState());
			value.Child.Tell(req.BuildInfo);
			return;
		}
		var child = Context.DIActorOf<DiscussionActor>(req.BuildConfigId.ToString());
		var state = new DiscussionData(child, new ReplaySubject<BuildDiscussionState>(1),
			new ReplaySubject<IImmutableList<DiscussionBuildData>>(1));
		state.Builds.OnNext(ImmutableList<DiscussionBuildData>.Empty);
		_discussions.Add(req.BuildConfigId, state);
		child.Tell(state);
		child.Tell(req.BuildConfig);
		child.Tell(new ActorsApi.BuildInfoItem(req.BuildInfo, req.BuildConfigId));
		child.Forward(new DiscussionActorApi.SubscribeForState());
		child.Tell(new DiscussionActorApi.SubscribeForComments(), _mentionsMonitor);
	}

	private void CloseDiscussion(ActorsApi.CloseDiscussion req) {
		if (_discussions.Remove(req.BuildConfigId, out var state)) {
			Context.Stop(state.Child);
		}
	}

	private void FindDiscussion(ActorsApi.FindDiscussion req) {
		if (_discussions.TryGetValue(req.BuildConfigId, out var state)) {
			var child = state.Child;
			if (!child.IsNobody()) {
				Sender.Tell(new ActorsApi.DiscussionHandle(true, child, state.Subject, state.Builds));
				return;
			}
		}
		Sender.Tell(ActorsApi.DiscussionHandle.Empty);
	}
}
