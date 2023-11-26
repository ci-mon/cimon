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
            var delta = mention.CommentsCount;
            var mentionInfo = _mentions.Find(x=>x.BuildConfigId == mention.BuildConfigId);
            if (mentionInfo is not null) {
                var newValue = mentionInfo with { CommentsCount = mentionInfo.CommentsCount + delta };
                _mentions = newValue.CommentsCount == 0 ? _mentions.Remove(mentionInfo) : _mentions.Replace(mentionInfo, newValue);
            } else {
                _mentions = _mentions.Add(mention);
            }
            _mentionsSubject.OnNext(_mentions);
        });
        Receive<ActorsApi.GetMentions>(_ => Sender.Tell(_mentionsSubject));
    }
}