using System.Collections.Immutable;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Discussions;

namespace Cimon.Data.Actors;

public class UserActor : ReceiveActor
{
    private ImmutableList<MentionInfo> _mentions = ImmutableList<MentionInfo>.Empty;
    private readonly ReplaySubject<IImmutableList<MentionInfo>> _mentionsSubject = new(1);
    public UserActor() {
        Receive<MentionInfo>(mention => {
            _mentions = _mentions.Add(mention);
            _mentionsSubject.OnNext(_mentions);
        });
        Receive<ActorsApi.GetMentions>(_ => Sender.Tell(_mentionsSubject));
    }
}