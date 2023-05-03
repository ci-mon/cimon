using System.Collections.Immutable;
using Optional;
using Optional.Collections;

namespace Cimon.Data;

public interface IBuildMonitoringService
{
	Task CheckBuildInfo(ImmutableArray<BuildInfo> buildInfos);
}

public class BuildMonitoringService : IBuildMonitoringService
{
	private readonly BuildDiscussionStoreService _discussionStore;
	private Option<ImmutableArray<BuildInfo>> _previousState;

	public BuildMonitoringService(BuildDiscussionStoreService discussionStore) {
		_discussionStore = discussionStore;
	}

	public async Task CheckBuildInfo(ImmutableArray<BuildInfo> buildInfos) {
		foreach (var current in buildInfos) {
			await _previousState.FlatMap(x => x.FirstOrNone(x => x.BuildId == current.BuildId))
				.Match(async previous => {
					if (previous.CanHaveDiscussion() && !current.CanHaveDiscussion()) {
						await _discussionStore.CloseDiscussion(current.BuildId);
					} else if (!previous.CanHaveDiscussion() && current.CanHaveDiscussion()) {
						await OpenDiscussion(current);
					}
				}, async () => {
					if (current.CanHaveDiscussion()) {
						await OpenDiscussion(current);
					}
				});
		}
		_previousState = buildInfos.Some();
	}

	private async Task OpenDiscussion(BuildInfo current) {
		var discussion = await _discussionStore.OpenDiscussion(current.BuildId);
		await discussion.MatchSomeAsync(x => x.AddComment(new CommentData {
			Author = "Monitor bot",
			Comment = BuildCommentMessage(current)
		}));
	}

	private string BuildCommentMessage(BuildInfo buildInfo) {
		return
			$"<p>Build failed by: {string.Join(", ", buildInfo.CommitterUsers.Select(u => GetUserMention(u.Name, u.Name)))}</p>";
	}

	private string GetUserMention(string userId, string userName) {
		return
			$"""<span class="mention" data-index="1" data-denotation-char="@" data-id="{userId}" data-value="{userName}"><span contenteditable="false"><span class="ql-mention-denotation-char">@</span>{userName}</span></span> """;
	}
}
