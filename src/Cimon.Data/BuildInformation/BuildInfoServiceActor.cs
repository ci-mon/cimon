﻿using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Common;
using Cimon.DB.Models;

namespace Cimon.Data.BuildInformation;

static class BuildInfoServiceActorApi
{
	public record Subscribe(BuildConfig BuildConfig);
	public record Unsubscribe(int BuildConfigId);
}

public class BuildInfoServiceActor : ReceiveActor
{
	public BuildInfoServiceActor(IActorRef discussionsService) {
		var mlActor = Context.DIActorOf<BuildMLActor>("ML");
		Receive<BuildInfoServiceActorApi.Subscribe>(msg => {
			Context.GetOrCreateChild<BuildInfoActor>(msg.BuildConfig.Id.ToString(), msg.BuildConfig.Id, mlActor)
				.Forward(msg);
		});
		Receive<BuildInfoServiceActorApi.Unsubscribe>(msg => {
			Context.Child(msg.BuildConfigId.ToString()).Forward(msg);
		});
		Receive<ActorsApi.OpenDiscussion>(discussionsService.Forward);
		Receive<ActorsApi.CloseDiscussion>(discussionsService.Forward);
	}
}
