using Akka.Actor;
using Cimon.Data.Common;

namespace Cimon.Data.Users;

class UserSupervisorActor : ReceiveActor
{
    public UserSupervisorActor() {
        Receive<ActorsApi.UserMessage>(msg => {
            var child = Context.GetOrCreateChild<UserActor>(msg.UserName);
            child.Forward(msg);
        });
    }
}
