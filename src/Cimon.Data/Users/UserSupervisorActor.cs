using System.Collections.Immutable;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Contracts;
using Cimon.Data.Common;

namespace Cimon.Data.Users;

using Microsoft.Extensions.Logging;

public class UserSupervisorActor : ReceiveActor
{
    public UserSupervisorActor(ILogger<UserSupervisorActor> logger) {
        var activeUserNames =
            new BehaviorSubject<IImmutableSet<string>>(
                ImmutableHashSet<string>.Empty.WithComparer(StringComparer.OrdinalIgnoreCase));
        Receive<ActorsApi.UserMessage>(msg => {
            if (string.IsNullOrWhiteSpace(msg.UserName)) {
                logger.LogDebug("Message to empty user {Message}", msg);
                return;
            }
            var child = Context.GetOrCreateChild<UserActor>(msg.UserName);
            child.Forward(msg);
            Context.Watch(child);
        });
        Receive<Terminated>(child => 
            activeUserNames.OnNext(activeUserNames.Value.Remove(child.ActorRef.Path.Name)));
        Receive<ActorsApi.GetActiveUserNames>(_ => Sender.Tell(activeUserNames));
        var userData = new Dictionary<int, UserConnectionInfo>();
        Receive<ActorsApi.UserConnected>(connected => {
            var user = connected.User;
            if (!userData.TryGetValue(user.Id, out var info)) {
                info = new UserConnectionInfo(user, new HashSet<string>());
                userData[user.Id] = info;
            }
            info.Connections.Add(connected.ConnectionId);
            activeUserNames.OnNext(activeUserNames.Value.Add(user.Name.Name));
        });
        Receive<ActorsApi.UserDisconnected>(disconnected => {
            if (!userData.TryGetValue(disconnected.User.Id, out var info)) {
                return;
            }
            info.Connections.Remove(disconnected.ConnectionId);
            if (!info.Connections.Any()) {
                Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(15), Self, 
                    new FullyDisconnected(info), Self);
            }
        });
        Receive<FullyDisconnected>(disconnected => {
            if (disconnected.ConnectionInfo.Connections.Count == 0) {
                var user = disconnected.ConnectionInfo.User;
                var userId = user.Id;
                if (userData.ContainsKey(userId)) {
                    userData.Remove(userId);
                }
                activeUserNames.OnNext(activeUserNames.Value.Remove(user.Name.Name));
            }
        });
    }

    record FullyDisconnected(UserConnectionInfo ConnectionInfo);
    record UserConnectionInfo(User User, HashSet<string> Connections);
}
