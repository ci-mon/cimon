using System.Collections.Immutable;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Actors;
using Cimon.Data.Discussions;

namespace Cimon.Data.Users;

public class UserActor : ReceiveActor
{
    private ImmutableList<MentionInfo> _mentions = ImmutableList<MentionInfo>.Empty;
    private readonly ReplaySubject<IImmutableList<MentionInfo>> _mentionsSubject = new(1);
    public UserActor() {
        Receive<MentionInfo>(mention => {
            var mentionInfo = _mentions.Find(x=>x.DiscussionId == mention.DiscussionId);
            _mentions = mentionInfo is not null
                ? _mentions.Replace(mentionInfo, mentionInfo with { Count = mentionInfo.Count + 1 })
                : _mentions.Add(mention);
            _mentionsSubject.OnNext(_mentions);
        });
        Receive<ActorsApi.GetMentions>(_ => Sender.Tell(_mentionsSubject));
    }
}