using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Common;

namespace Cimon.Data.Discussions;

public class DiscussionStoreActor : ReceiveActor
{
	private readonly IActorRef _mentionsMonitor;

	record State(IActorRef Child, ReplaySubject<BuildDiscussionState> Subject, bool Merged);
	private readonly Dictionary<int, State> _discussions = new();
	private readonly Dictionary<IActorRef, State> _stateMap = new();
	public DiscussionStoreActor(IActorRef mentionsMonitor) {
		_mentionsMonitor = mentionsMonitor;
		Receive<ActorsApi.FindDiscussion>(FindDiscussion);
		Receive<ActorsApi.CloseDiscussion>(CloseDiscussion);
		Receive<ActorsApi.OpenDiscussion>(OpenDiscussion);
		Receive<BuildDiscussionState>(newState => {
			if (_stateMap.TryGetValue(Sender, out var state)) {
				state.Subject.OnNext(newState);
			}
		});
	}

	private void OpenDiscussion(ActorsApi.OpenDiscussion req) {
		if (_discussions.TryGetValue(req.BuildConfigId, out var value)) {
			Sender.Tell(value.Child);
			return;
		}
		var child = Context.DIActorOf<DiscussionActor>(req.BuildConfigId.ToString());
		var state = new State(child, new ReplaySubject<BuildDiscussionState>(1), false);
		_stateMap[child] = state;
		_discussions.Add(req.BuildConfigId, state);
		child.Tell(req.BuildInfo);
		child.Forward(new DiscussionActorApi.SubscribeForState());
		child.Tell(new DiscussionActorApi.SubscribeForComments(), _mentionsMonitor);
		Sender.Tell(child);
	}

	private void CloseDiscussion(ActorsApi.CloseDiscussion req) {
		if (_discussions.Remove(req.BuildConfigId, out var state)) {
			_stateMap.Remove(state.Child);
			if (state.Merged) return;
			Context.Stop(state.Child);
			state.Subject.OnCompleted();
			state.Subject.Dispose();
			Context.Stop(Self);
		}
	}

	private void FindDiscussion(ActorsApi.FindDiscussion req) {
		if (_discussions.TryGetValue(req.BuildConfigId, out var state)) {
			var child = state.Child;
			if (!child.IsNobody()) {
				Sender.Tell(new ActorsApi.DiscussionHandle(true, child, state.Subject));
				return;
			}
		}
		Sender.Tell(ActorsApi.DiscussionHandle.Empty);
	}
}
