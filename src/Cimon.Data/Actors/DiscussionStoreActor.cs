using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Discussions;

namespace Cimon.Data.Actors;

public class DiscussionStoreActor : ReceiveActor
{
	record State(IActorRef Child, ReplaySubject<BuildDiscussionState> Subject, bool Merged);
	private readonly Dictionary<int, State> _discussions = new();
	private readonly Dictionary<IActorRef, State> _stateMap = new();
	public DiscussionStoreActor() {
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
		if (_discussions.ContainsKey(req.BuildConfigId)) {
			return;
		}
		var child = Context.DIActorOf<DiscussionActor>(req.BuildConfigId.ToString());
		var state = new State(child, new ReplaySubject<BuildDiscussionState>(1), false);
		_stateMap[child] = state;
		_discussions.Add(req.BuildConfigId, state);
		child.Forward(req.BuildInfo);
		child.Forward(new DiscussionActorApi.Subscribe());
	}

	private void CloseDiscussion(ActorsApi.CloseDiscussion req) {
		if (_discussions.Remove(req.BuildConfigId, out var state)) {
			_stateMap.Remove(state.Child);
			if (state.Merged) return;
			Context.Stop(state.Child);
			state.Subject.OnCompleted();
			state.Subject.Dispose();
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
