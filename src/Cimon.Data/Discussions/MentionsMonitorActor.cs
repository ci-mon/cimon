using Akka.Actor;
using Cimon.Data.Actors;

namespace Cimon.Data.Discussions;

public class MentionsMonitorActor : ReceiveActor
{
    private readonly Dictionary<IActorRef, List<ActorsApi.UserMessage<MentionInfo>>> _compensations = new();
    public MentionsMonitorActor(IActorRef userServiceActor) {
        Receive<BuildCommentChange>(state => {
            var sender = Sender;
            if (!_compensations.TryGetValue(sender, out var res)) {
                res = new List<ActorsApi.UserMessage<MentionInfo>>();
                _compensations[sender] = res;
                Context.Watch(sender);
            }
            foreach (var entityId in state.Comment.Mentions) {
                var msg = new ActorsApi.UserMessage<MentionInfo>(entityId.Name, new MentionInfo(state.DiscussionId, 1));
                userServiceActor.Tell(msg);
                res.Add(msg with { Payload = msg.Payload  with { Count = -1 } });
            }
        });
        Receive<Terminated>(terminated => {
            if (!_compensations.Remove(terminated.ActorRef, out var res)) return;
            foreach (var data in res) {
                userServiceActor.Tell(data);
            }
        });
    }
}