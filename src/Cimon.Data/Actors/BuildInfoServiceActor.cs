using Akka.Actor;
using Cimon.DB.Models;

namespace Cimon.Data.Actors;

static class BuildInfoServiceActorApi
{
	public record Subscribe(BuildConfig BuildConfig);
	public record Unsubscribe(string BuildConfigId);
}
public class BuildInfoServiceActor : ReceiveActor
{
	public BuildInfoServiceActor(IActorRef discussionsService) {
		Receive<BuildInfoServiceActorApi.Subscribe>(msg => {
			Context.GetOrCreateChild<BuildInfoActor>(msg.BuildConfig.Id.ToString())
				.Forward(msg.BuildConfig);
		});
		Receive<BuildInfoServiceActorApi.Unsubscribe>(msg => {
			Context.Child(msg.BuildConfigId.ToString()).Forward(msg);
		});
		Receive<ActorsApi.OpenDiscussion>(discussionsService.Forward);
		Receive<ActorsApi.CloseDiscussion>(discussionsService.Forward);
	}
}
