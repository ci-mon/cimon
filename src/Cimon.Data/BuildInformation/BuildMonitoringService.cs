using System.Collections.Immutable;
using System.Reactive.Linq;
using Cimon.Data.Discussions;
using MediatR;
using Optional;
using Optional.Collections;

namespace Cimon.Data.BuildInformation;

using Cimon.Contracts.CI;

public class BuildMonitoringService : IBuildMonitoringService
{
	private readonly BuildDiscussionStoreService _discussionStore;
	private Option<IImmutableList<BuildInfo>> _previousState;
	private readonly IMediator _mediator;

	public BuildMonitoringService(BuildDiscussionStoreService discussionStore, IMediator mediator) {
		_discussionStore = discussionStore;
		_mediator = mediator;
	}

	public async Task CheckBuildInfo(IImmutableList<BuildInfo> buildInfos) {
		foreach (var current in buildInfos) {
			await _previousState.FlatMap(x => x.FirstOrNone(x => x.BuildConfigId == current.BuildConfigId))
				.Match(async previous => {
					if (previous.CanHaveDiscussion() && !current.CanHaveDiscussion()) {
						var discussion = await _discussionStore.GetDiscussionService(current.BuildConfigId).FirstAsync();
						await _mediator.Publish(new DiscussionClosedNotification(discussion));
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

	
	private async Task OpenDiscussion(BuildInfo current) {
		var discussion = await _discussionStore.OpenDiscussion(current.BuildConfigId);
		discussion.MatchSome(x => {
			_mediator.Publish(new DiscussionOpenNotification(x, current));
		});
	}

}
