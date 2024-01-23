using Akka.Actor;
using Cimon.Data.Common;

namespace Cimon.Data.Users;

using Microsoft.Extensions.Logging;

class UserSupervisorActor : ReceiveActor
{
    public UserSupervisorActor(ILogger<UserSupervisorActor> logger) {
        Receive<ActorsApi.UserMessage>(msg => {
            if (string.IsNullOrWhiteSpace(msg.UserName)) {
                logger.LogDebug("Message to empty user {Message}", msg);
                return;
            }
            var child = Context.GetOrCreateChild<UserActor>(msg.UserName);
            child.Forward(msg);
        });
    }
}
