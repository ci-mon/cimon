using System.Collections.Immutable;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Discussions;

namespace Cimon.Data.Users;

public class UserActor : ReceiveActor
{
    private ImmutableList<MentionInfo> _mentions = ImmutableList<MentionInfo>.Empty;
    private readonly ReplaySubject<IImmutableList<MentionInfo>> _mentionsSubject = new(1);
    public UserActor() {
        Receive<MentionInfo>(mention => {
            var delta = mention.Count;
            var mentionInfo = _mentions.Find(x=>x.BuildConfigId == mention.BuildConfigId);
            if (mentionInfo is not null) {
                var newValue = mentionInfo with { Count = mentionInfo.Count + delta };
                _mentions = newValue.Count == 0 ? _mentions.Remove(mentionInfo) : _mentions.Replace(mentionInfo, newValue);
            } else {
                _mentions = _mentions.Add(mention);
            }
            _mentionsSubject.OnNext(_mentions);
        });
        Receive<ActorsApi.GetMentions>(_ => Sender.Tell(_mentionsSubject));
    }
}