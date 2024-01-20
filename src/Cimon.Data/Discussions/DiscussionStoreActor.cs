using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Common;

namespace Cimon.Data.Discussions;

using Cimon.Contracts.CI;
using Cimon.Data.BuildInformation;

record DiscussionData(IActorRef Child, ReplaySubject<BuildDiscussionState> Subject,
	ReplaySubject<BuildInfo> BuildInfo, ReplaySubject<BuildConfig> BuildConfig);

public class DiscussionStoreActor : ReceiveActor
{
	private readonly IActorRef _mentionsMonitor;
	private readonly Dictionary<int, DiscussionData> _discussions = new();
	private readonly Dictionary<IActorRef, DiscussionData> _stateMap = new();
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
			value.Child.Forward(new DiscussionActorApi.SubscribeForState());
			value.Child.Tell(req.BuildInfo);
			return;
		}
		var child = Context.DIActorOf<DiscussionActor>(req.BuildConfigId.ToString());
		var state = new DiscussionData(child, new ReplaySubject<BuildDiscussionState>(1),
			new ReplaySubject<BuildInfo>(1), new ReplaySubject<BuildConfig>(1));
		_stateMap[child] = state;
		_discussions.Add(req.BuildConfigId, state);
		child.Tell(state);
		child.Tell(req.BuildConfig);
		child.Tell(req.BuildInfo);
		child.Forward(new DiscussionActorApi.SubscribeForState());
		child.Tell(new DiscussionActorApi.SubscribeForComments(), _mentionsMonitor);
		AppActors.Instance.BuildInfoService.Tell(new BuildInfoServiceActorApi.Subscribe(req.BuildConfig), 
			child);
	}

	private void CloseDiscussion(ActorsApi.CloseDiscussion req) {
		if (_discussions.Remove(req.BuildConfigId, out var state)) {
			_stateMap.Remove(state.Child);
			Context.Stop(state.Child);
			state.Subject.OnCompleted();
			state.Subject.Dispose();
		}
	}

	private void FindDiscussion(ActorsApi.FindDiscussion req) {
		if (_discussions.TryGetValue(req.BuildConfigId, out var state)) {
			var child = state.Child;
			if (!child.IsNobody()) {
				Sender.Tell(new ActorsApi.DiscussionHandle(true, child, state.Subject, state.BuildInfo,
					state.BuildConfig));
				return;
			}
		}
		Sender.Tell(ActorsApi.DiscussionHandle.Empty);
	}
}
