﻿using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Common;

namespace Cimon.Data.Discussions;

using Cimon.Contracts.CI;
using Cimon.Data.BuildInformation;

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
			new ReplaySubject<IImmutableList<DiscussionBuildData>>(1));
		state.Builds.OnNext(ImmutableList<DiscussionBuildData>.Empty);
		_stateMap[child] = state;
		_discussions.Add(req.BuildConfigId, state);
		child.Tell(state);
		child.Tell(req.BuildConfig);
		child.Tell(new ActorsApi.BuildInfoItem(req.BuildInfo, req.BuildConfigId));
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
				Sender.Tell(new ActorsApi.DiscussionHandle(true, child, state.Subject, state.Builds));
				return;
			}
		}
		Sender.Tell(ActorsApi.DiscussionHandle.Empty);
	}
}
