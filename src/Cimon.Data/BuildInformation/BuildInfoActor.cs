using System.Reactive.Linq;
using Akka.Actor;
using Akka.Event;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.DB.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.BuildInformation;

class BuildInfoActor : ReceiveActor
{
	private readonly int _buildConfigId;
	private readonly BuildConfigService _buildConfigService;
	private readonly BuildInfoMonitoringSettings _settings;
	private readonly IActorRef _mlActor;

	record StopIfIdle;
	record GetBuildInfo;

	private readonly GetBuildInfo _getBuildInfo = new();
	private readonly StopIfIdle _stopIfIdle = new();
	private readonly List<IActorRef> _subscribers = new();
	private BuildConfigModel? _config;
	private IBuildInfoProvider? _provider;
	private ICancelable? _refreshBuildInfoScheduler;
	private readonly HashSet<string> _systemUserLogins;
	private readonly BuildInfoHistory _buildInfoHistory = new();
	private int _commentsCount;
	private bool _discussionWatched;
	private bool _discussionOpen;
	private readonly ILoggingAdapter _log = Context.GetLogger();
	private readonly IServiceScope _scope;
	private CIConnectorInfo _connectorInfo;

	public BuildInfoActor(int buildConfigId, IActorRef mlActor, IServiceProvider serviceProvider, BuildConfigService buildConfigService,
			BuildInfoMonitoringSettings settings) {
		_mlActor = mlActor;
		_scope = serviceProvider.CreateScope();
		_buildConfigId = buildConfigId;
		_buildConfigService = buildConfigService;
		_settings = settings;
		_systemUserLogins = new HashSet<string>(settings.SystemUserLogins, StringComparer.OrdinalIgnoreCase);
		Receive<BuildInfoServiceActorApi.Subscribe>(OnSubscribe);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(Unsubscribe);
		Receive<StopIfIdle>(OnStopIfIdle);
		ReceiveAsync<BuildConfigModel>(InitBuildConfig);
		ReceiveAsync<GetBuildInfo>(OnGetBuildInfo);
		Receive<BuildInfo>(HandleBuildInfo);
		Receive<BuildFailureSuspect>(HandleBuildFailureSuspect);
		Receive<NotifySubscribersMsg>(_ => {
			if (_buildInfoHistory.Last is not { } buildInfo) return;
			NotifySubscribers(buildInfo);
		});
		Receive<Terminated>(_ => {
			_discussionOpen = false;
		});
		Receive<BuildDiscussionState>(state => {
			if (!_discussionWatched) {
				_discussionWatched = true;
				Context.Watch(Sender);
			}
			_commentsCount = state.Comments.Count;
			NotifySubscribers(_buildInfoHistory.Last);
		});
		var buildConfigStream =
			buildConfigService.BuildConfigs.Select(x => x.FirstOrDefault(c => c.Id == _buildConfigId));
		Context.Observe(buildConfigStream);
	}

	protected override void PostStop() {
		base.PostStop();
		_scope?.Dispose();
	}

	private void OnSubscribe(BuildInfoServiceActorApi.Subscribe msg) {
		_subscribers.Add(Sender);
		if (_buildInfoHistory.Last is {} current) {
			Sender.Tell(CreateNotification(current));
		}
	}

	private ActorsApi.BuildInfoItem CreateNotification(BuildInfo current) => new(current, _config!.Id);

	private async Task InitBuildConfig(BuildConfigModel config) {
		try {
			_config = config;
			_provider = _scope.ServiceProvider.GetRequiredKeyedService<IBuildInfoProvider>(config.Connector.CISystem);
			_connectorInfo = await _buildConfigService.GetConnectorInfo(config.Connector);
			_refreshBuildInfoScheduler ??= Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
				TimeSpan.Zero, _settings.Delay, Self, _getBuildInfo, Self);
		} catch (Exception e) {
			_log.Error("Failed to InitBuildConfig", e);
			Context.Stop(Self);
		}
	}

	private void HandleBuildFailureSuspect(BuildFailureSuspect suspect) {
		var buildInfo = _buildInfoHistory.Last;
		if (buildInfo != null) {
			buildInfo.FailureSuspect = suspect;
			NotifySubscribers(buildInfo);
		}
	}

	private void HandleBuildInfo(BuildInfo? newInfo) {
		if (newInfo is null) return;
		if (_config!.DemoState is null && _buildInfoHistory.Last?.Id.Equals(newInfo.Id) is true) {
			return;
		}
		newInfo.Changes = newInfo.Changes.Where(x => !_systemUserLogins.Contains(x.Author.Name)).ToList();
		_buildInfoHistory.Add(newInfo);
		_mlActor.Tell(new MlRequest(_connectorInfo, _config, _provider!, newInfo, Self));
		HandleDiscussion(newInfo);
		DelayedNotifySubscribers();
	}

	private async Task OnGetBuildInfo(GetBuildInfo _) {
		if (_config!.DemoState is not null) {
			var buildInfo = _config.DemoState with {
				Duration = TimeSpan.FromMinutes(Random.Shared.Next(120)),
				Id = Random.Shared.Next(1000).ToString()
			};
			Self.Tell(buildInfo);
			return;
		}
		try {
			var options = new BuildInfoQueryOptions {
				LastBuildId = _buildInfoHistory.Last?.Id
			};
			var query = new BuildInfoQuery(_connectorInfo, _config!, options);
			var infos = await _provider!.FindInfo(query);
			if (!infos.Any()) {
				Self.Tell(BuildInfo.NoData);
				return;
			}
			foreach (var buildInfo in infos) {
				Self.Tell(buildInfo);
			}
		} catch (Exception e) {
			_log.Error(e, e.Message);
		}
	}

	private sealed record NotifySubscribersMsg;
	private readonly NotifySubscribersMsg _notifySubscribersMsg = new();
	private void DelayedNotifySubscribers() {
		var delay = _settings.Delay / 2;
		var tenSeconds = TimeSpan.FromSeconds(10);
		if (delay > tenSeconds) {
			delay = tenSeconds;
		}
		Context.System.Scheduler.ScheduleTellOnce(delay, Self, _notifySubscribersMsg, Self);
	}

	private void NotifySubscribers(BuildInfo? current) {
		if (current is null) return;
		current.CommentsCount = _commentsCount;
		var buildInfoItem = CreateNotification(current);
		_subscribers.ForEach(s => s.Tell(buildInfoItem));
	}

	private void HandleDiscussion(BuildInfo current) {
		var canHaveDiscussion = current.CanHaveDiscussion();
		if (_discussionOpen && !canHaveDiscussion) {
			Context.Parent.Tell(new ActorsApi.CloseDiscussion(_config!.Id));
			_discussionOpen = false;
		} else if (!_discussionOpen && canHaveDiscussion) {
			Context.Parent.Tell(new ActorsApi.OpenDiscussion(_config!, current));
			_discussionOpen = true;
		}
	}

	private void OnStopIfIdle(StopIfIdle _) {
		if (_subscribers.Count == 0) {
			_refreshBuildInfoScheduler?.Cancel();
			Self.Tell(PoisonPill.Instance);
		}
	}

	private void Unsubscribe(BuildInfoServiceActorApi.Unsubscribe _) {
		_subscribers.Remove(Sender);
		if (_subscribers.Count == 0) {
			var delay = TimeSpan.FromSeconds(30);
			Context.System.Scheduler.ScheduleTellOnce(delay, Self, _stopIfIdle, Self);
		}
	}
}
