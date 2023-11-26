using Akka.Actor;

namespace Cimon.Data.Discussions;

public class MentionsMonitorActor : ReceiveActor
{
    private readonly Dictionary<IActorRef, List<ActorsApi.UserMessage<MentionInfo>>> _compensations = new();
    public MentionsMonitorActor(IActorRef userServiceActor) {
        Receive<BuildCommentChange>(state => {
            var sender = Sender;
            if (!_compensations.TryGetValue(sender, out var compensation)) {
                compensation = new List<ActorsApi.UserMessage<MentionInfo>>();
                _compensations[sender] = compensation;
                Context.Watch(sender);
            }
            foreach (var entityId in state.Comment.Mentions) {
                var countDelta = state.ChangeType switch {
                    ChangeType.Add => 1,
                    ChangeType.Remove => -1,
                    _ => 0
                };
                var msg = new ActorsApi.UserMessage<MentionInfo>(entityId.Name, new MentionInfo(state.BuildConfigId, countDelta));
                userServiceActor.Tell(msg);
                compensation.Add(msg with { Payload = msg.Payload  with { CommentsCount = -1 * countDelta } });
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