using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cimon.Contracts;

namespace Cimon.Data.Discussions;

public class MentionsService
{
	private readonly BuildDiscussionStoreService _discussionStore;
	public MentionsService(BuildDiscussionStoreService discussionStore) {
		_discussionStore = discussionStore;
	}

	public IObservable<int> GetMentionsCount(User user) => GetMentions(user).Select(x => x.Sum(m => m.CommentsCount));

	public IObservable<IReadOnlyCollection<MentionInfo>> GetMentions(User user) {
		var buffer = new BehaviorSubject<List<MentionInfo>>(new List<MentionInfo>());
		var buildCommentsObs = _discussionStore.AllDiscussions.SelectMany(x => x)
			.SelectMany(x => x.Comments.Select(c => (x.BuildId, c)));
		return buffer.CombineLatest(buildCommentsObs).Select(tuple => {
			var (mentions, (buildId, allComments)) = tuple;
			var buildComments = allComments.Where(c => c.Mentions.Any(m => m.Name == user.Name.Name)).ToList();
			var existing = mentions.Find(x => x.BuildId == buildId);
			if (existing == null) {
				existing = new MentionInfo(buildId, buildComments.Count);
				mentions.Add(existing);
			} else if (buildComments.Count == 0) {
				mentions.Remove(existing);
			}
			else {
				existing.CommentsCount = buildComments.Count;
			}
			return mentions;
		}).Select(x=>x.Where(i=>i.CommentsCount > 0).ToList());
	}
}