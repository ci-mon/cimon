using Akka.Actor;
using Akka.Event;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.BuildInformation;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.DB.Models;

namespace Cimon.Data.Actors;

class BuildInfoActor : ReceiveActor
{
	private readonly IEnumerable<IBuildInfoProvider> _buildInfoProviders;
	private readonly BuildInfoMonitoringSettings _settings;

	record StopIfIdle;
	record GetBuildInfo;

	private readonly GetBuildInfo _getBuildInfo = new();
	private readonly StopIfIdle _stopIfIdle = new();
	private readonly List<IActorRef> _subscribers = new();
	private BuildConfig? _config;
	private IBuildInfoProvider? _provider;
	private ICancelable? _refreshBuildInfoScheduler;
	private readonly HashSet<string> _systemUserLogins;
	private readonly RingBuffer<BuildInfo> _buildInfos = new(50);
	private int _commentsCount;
	private bool _discussionWatched;

	public BuildInfoActor(IEnumerable<IBuildInfoProvider> buildInfoProviders,
			BuildInfoMonitoringSettings settings) {
		_buildInfoProviders = buildInfoProviders;
		_settings = settings;
		_systemUserLogins = new HashSet<string>(settings.SystemUserLogins, StringComparer.OrdinalIgnoreCase);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(Unsubscribe);
		Receive<StopIfIdle>(OnStopIfIdle);
		Receive<BuildConfig>(InitBuildConfig);
		Receive<GetBuildInfo>(OnGetBuildInfo);
		Receive<BuildInfo>(HandleBuildInfo);
		Receive<Status.Failure>(failure => {
			Context.GetLogger().Error(failure.Cause, failure.Cause.Message);
		});
		Receive<Terminated>(terminated => {
			_discussionOpen = false;
		});
		Receive<BuildDiscussionState>(state => {
			if (!_discussionWatched) {
				_discussionWatched = true;
				Context.Watch(Sender);
			}
			_commentsCount = state.Comments.Count;
			NotifySubscribers(_buildInfos.Last);
		});
	}

	private void InitBuildConfig(BuildConfig config) {
		_subscribers.Add(Sender);
		if (_config == null) {
			_config = config;
			_provider = _buildInfoProviders.Single(p => p.CiSystem == config.CISystem);
			_refreshBuildInfoScheduler = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
				TimeSpan.FromSeconds(Random.Shared.Next(0, 10)), _settings.Delay, Self, _getBuildInfo, Self);
		}
	}

	private bool _discussionOpen;
	private void HandleBuildInfo(BuildInfo? current) {
		if (current is null) return;
		HandleDiscussion(current);
		if (_buildInfos.Last?.Number.Equals(current.Number) is true) {
			return;
		}
		_buildInfos.Add(current);
		current.Changes = current.Changes.Where(x => !_systemUserLogins.Contains(x.Author.Name)).ToList();
		NotifySubscribers(current);
	}

	private void NotifySubscribers(BuildInfo? current) {
		if (current is null) return;
		current.CommentsCount = _commentsCount;
		_subscribers.ForEach(s => s.Tell(current));
	}

	private void HandleDiscussion(BuildInfo current) {
		var canHaveDiscussion = current.CanHaveDiscussion();
		if (_discussionOpen && !canHaveDiscussion) {
			Context.Parent.Tell(new ActorsApi.CloseDiscussion(_config!.Id));
			_discussionOpen = false;
		} else if (!_discussionOpen && canHaveDiscussion) {
			Context.Parent.Tell(new ActorsApi.OpenDiscussion(_config!.Id, current));
			_discussionOpen = true;
		}
	}

	private void OnGetBuildInfo(GetBuildInfo _) {
		var options = new BuildInfoQueryOptions();
		var query = new BuildInfoQuery(_config!, options);
		if (_config!.DemoState is not null) {
			_config.DemoState.BuildConfigId = _config.Id;
			_config.DemoState.Duration = TimeSpan.FromMinutes(Random.Shared.Next(120));
			Self.Tell(_config.DemoState);
			return;
		}
		_provider!.FindInfo(query).PipeTo(Self);
	}

	private void OnStopIfIdle(StopIfIdle _) {
		if (_subscribers.Count == 0) {
			_refreshBuildInfoScheduler?.Cancel();
			Self.Tell(PoisonPill.Instance);
		}
	}

	private void Unsubscribe(BuildInfoServiceActorApi.Unsubscribe _) {
		_subscribers.Remove(Sender);
		if (_subscribers.Count == 0)
			Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), Self, _stopIfIdle, Self);
	}
}
