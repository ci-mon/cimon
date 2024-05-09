using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Common;

namespace Cimon.Data.BuildInformation;

static class BuildInfoServiceActorApi
{
	public record Subscribe(BuildConfig BuildConfig);
	public record Refresh(BuildConfig BuildConfig);
	public record Unsubscribe(int BuildConfigId);
}

public class BuildInfoServiceActor : ReceiveActor
{
	private readonly IActorRef _mlActor;

	public BuildInfoServiceActor(IActorRef discussionsService) {
		_mlActor = Context.DIActorOf<BuildMLActor>("ML");
		Receive<BuildInfoServiceActorApi.Subscribe>(msg => ForwardToChild(msg.BuildConfig, msg));
		Receive<BuildInfoServiceActorApi.Refresh>(msg => ForwardToChild(msg.BuildConfig, msg));
		Receive<BuildInfoServiceActorApi.Unsubscribe>(msg => Context.Child(msg.BuildConfigId.ToString()).Forward(msg));
		Receive<ActorsApi.Discussions.BuildStatusChanged>(discussionsService.Forward);
	}

	private void ForwardToChild(BuildConfig buildConfig, object msg) {
		Context.GetOrCreateChild<BuildInfoActor>(buildConfig.Id.ToString(), buildConfig.Id, _mlActor)
			.Forward(msg);
	}
}
