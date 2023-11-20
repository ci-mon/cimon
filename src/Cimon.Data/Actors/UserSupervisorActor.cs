using Akka.Actor;

namespace Cimon.Data.Actors;

class UserSupervisorActor : ReceiveActor
{
    public UserSupervisorActor() {
        Receive<ActorsApi.UserMessage>(msg => {
            var child = Context.GetOrCreateChild<UserActor>(msg.UserName);
            child.Forward(msg.Message);
        });
    }
}