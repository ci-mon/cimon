using System.Collections.Immutable;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.Users;
using Optional;
using Optional.Collections;

namespace Cimon.Data.BuildInformation;

public class BuildMonitoringService : IBuildMonitoringService
{
	private readonly BuildDiscussionStoreService _discussionStore;
	private Option<IImmutableList<Contracts.BuildInfo>> _previousState;
	private readonly ITechnicalUsers _technicalUsers;

	public BuildMonitoringService(BuildDiscussionStoreService discussionStore, ITechnicalUsers technicalUsers) {
		_discussionStore = discussionStore;
		_technicalUsers = technicalUsers;
	}

	public async Task CheckBuildInfo(IImmutableList<Contracts.BuildInfo> buildInfos) {
		foreach (var current in buildInfos) {
			await _previousState.FlatMap(x => x.FirstOrNone(x => x.BuildConfigId == current.BuildConfigId))
				.Match(async previous => {
					if (previous.CanHaveDiscussion() && !current.CanHaveDiscussion()) {
						await _discussionStore.CloseDiscussion(current.BuildConfigId);
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

	private async Task OpenDiscussion(Contracts.BuildInfo current) {
		var discussion = await _discussionStore.OpenDiscussion(current.BuildConfigId);
		await discussion.MatchSomeAsync(x => x.AddComment(new CommentData {
			Author = _technicalUsers.MonitoringBot,
			Comment = BuildCommentMessage(current)
		}));
	}

	private string BuildCommentMessage(Contracts.BuildInfo buildInfo) {
		var users = buildInfo.CommitterUsers;
		var values = users?.Select(u => GetUserMention(u.Name, u.Name)) ?? new[] { "somebody" };
		return $"<p>Build failed by: {string.Join(", ", values)}</p>";
	}

	private string GetUserMention(string userId, string userName) {
		return
			$"""<span class="mention" data-index="1" data-denotation-char="@" data-id="{userId}" data-value="{userName}"><span contenteditable="false"><span class="ql-mention-denotation-char">@</span>{userName}</span></span> """;
	}
}
