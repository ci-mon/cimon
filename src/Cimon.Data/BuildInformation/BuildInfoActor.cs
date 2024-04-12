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
	private CIConnectorInfo _connectorInfo = null!;

	public BuildInfoActor(int buildConfigId, IActorRef mlActor, IServiceProvider serviceProvider, 
			BuildConfigService buildConfigService,
			BuildInfoMonitoringSettings settings) {
		_mlActor = mlActor;
		_scope = serviceProvider.CreateScope();
		_buildConfigService = buildConfigService;
		_settings = settings;
		_systemUserLogins = new HashSet<string>(settings.SystemUserLogins, StringComparer.OrdinalIgnoreCase);
		Setup();
		var buildConfigStream = buildConfigService.Get(buildConfigId);
		Context.Observe(buildConfigStream);
		_log.Info($"BuildInfoActor started: {Self.Path.Name}");
	}

	private void Setup() {
		Receive<BuildInfoServiceActorApi.Subscribe>(OnSubscribe);
		ReceiveAsync<BuildInfoServiceActorApi.Refresh>(OnGetBuildInfo);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(Unsubscribe);
		Receive<StopIfIdle>(OnStopIfIdle);
		ReceiveAsync<BuildConfigModel>(InitBuildConfig);
		ReceiveAsync<GetBuildInfo>(OnGetBuildInfo);
		Receive<MlResponse>(HandleMlResponse);
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
	}

	protected override void PostStop() {
		_log.Info($"BuildInfoActor stopped: {Self.Path.Name}");
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
			var mlWasEnabled = _config?.AllowML ?? true;
			_config = config;
			_provider = _scope.ServiceProvider.GetRequiredKeyedService<IBuildInfoProvider>(config.Connector.CISystem);
			_connectorInfo = await _buildConfigService.GetConnectorInfo(config.Connector);
			_refreshBuildInfoScheduler ??= Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
				TimeSpan.Zero, _settings.Delay, Self, _getBuildInfo, Self);
			if (_buildInfoHistory.Last is { } buildInfo && !mlWasEnabled) {
				TryRunMl(buildInfo);
			}
		} catch (Exception e) {
			_log.Error("Failed to InitBuildConfig", e);
			Context.Stop(Self);
		}
	}

	private void HandleMlResponse(MlResponse response) {
		if (_buildInfoHistory.SetFailureSuspect(response.Request.BuildInfo.Id, response.Suspect)) {
			NotifySubscribers(_buildInfoHistory.Last);
		}
	}

	private bool AddBuildInfoToHistory(BuildInfo? newInfo) {
		if (newInfo is null) return true;
		var lastBuildId = _buildInfoHistory.Last?.Id;
		if (lastBuildId == newInfo.Id) {
			return false;
		}
		_buildInfoHistory.Add(newInfo);
		return true;
	}

	private void TryRunMl(BuildInfo newInfo) {
		if (!_config!.AllowML) {
			return;
		}
		if (!newInfo.IsNotOk()) {
			return;
		}
		var mlMsg = new MlRequest(_connectorInfo, _config, _provider!, newInfo, Self);
		_mlActor.Tell(mlMsg);
	}

	private async Task OnGetBuildInfo<T>(T _) {
		try {
			string? lastBuildId = _buildInfoHistory.Last?.Id;
			var options = new BuildInfoQueryOptions(lastBuildId, 5);
			var query = new BuildInfoQuery(_connectorInfo, _config!, options);
			var infos = await _provider!.FindInfo(query);
			if (!infos.Any() && lastBuildId is null) {
				Self.Tell(BuildInfo.NoData);
				return;
			}
			AddBuildInfos(infos);
		} catch (Exception e) {
			_log.Error(e, e.Message);
		}
	}

	private void AddBuildInfos(IReadOnlyList<BuildInfo> infos) {
		int lastId = infos.Count - 1;
		for (var index = 0; index <= lastId; index++) {
			BuildInfo buildInfo = infos[index];
			buildInfo.Changes = buildInfo.Changes.Where(x => !_systemUserLogins.Contains(x.Author.Name)).ToList();
			if (!AddBuildInfoToHistory(buildInfo)) {
				continue;
			}
			if (index == lastId) {
				TryRunMl(buildInfo);
				HandleDiscussion(buildInfo);
				NotifySubscribers(buildInfo);
			}
		}
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
