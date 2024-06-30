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
		Receive<BuildDiscussionState>(state => {
			_commentsCount = state.Comments.Count;
			NotifySubscribers(BuildInfoItemUpdateSource.DiscussionInfoChanged);
		});
	}

	protected override void PostStop() {
		_log.Info($"BuildInfoActor stopped: {Self.Path.Name}");
		base.PostStop();
		_scope?.Dispose();
	}

	private void OnSubscribe(BuildInfoServiceActorApi.Subscribe msg) {
		_subscribers.Add(Sender);
		if (_buildInfoHistory.CombinedInfo is {} current) {
			Sender.Tell(CreateNotification(current, BuildInfoItemUpdateSource.None));
		}
	}

	private ActorsApi.BuildInfoItem CreateNotification(BuildInfo current, BuildInfoItemUpdateSource updateSource) =>
		new(current, _config!.Id, updateSource);

	private async Task InitBuildConfig(BuildConfigModel config) {
		try {
			var mlWasEnabled = _config?.AllowML ?? true;
			_config = config;
			_provider = _scope.ServiceProvider.GetRequiredKeyedService<IBuildInfoProvider>(config.Connector.CISystem);
			_connectorInfo = await _buildConfigService.GetConnectorInfo(config.Connector);
			_refreshBuildInfoScheduler ??= Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
				TimeSpan.Zero, _settings.Delay, Self, _getBuildInfo, Self);
			if (_buildInfoHistory.CombinedInfo is { } buildInfo && !mlWasEnabled) {
				TryRunMl(buildInfo);
			}
		} catch (Exception e) {
			_log.Error("Failed to InitBuildConfig", e);
			Context.Stop(Self);
		}
	}

	private void HandleMlResponse(MlResponse response) {
		if (_buildInfoHistory.SetFailureSuspect(response.Request.BuildInfo.Id, response.Suspects)) {
			NotifySubscribers(BuildInfoItemUpdateSource.SuspectsChanged);
		}
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
			string? lastBuildId = _buildInfoHistory.CombinedInfo?.Id;
			var options = new BuildInfoQueryOptions(lastBuildId, 15, 5);
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
		var addedItems = new List<BuildInfoHistory.Item>();
		bool needUpdate = false;
		var lastId = _buildInfoHistory.CombinedInfo?.Id;
		foreach (var buildInfo in infos) {
			buildInfo.Changes = buildInfo.Changes.Where(x => !_systemUserLogins.Contains(x.Author.Name)).ToList();
			var item = buildInfo.Id == lastId ? null : _buildInfoHistory.Add(buildInfo);
			if (item is not null) {
				needUpdate = true;
				addedItems.Add(item);
			}
		}
		if (!needUpdate) return;
		_buildInfoHistory.InitializeLastBuildInfo();
		var unresolvedItems = addedItems.Where(i => !i.Resolved).ToList();
		foreach (var item in unresolvedItems) {
			if (!item.Stats.IsUnstable) {
				TryRunMl(item.Info);
			}
			HandleDiscussion(item);
		}
		NotifySubscribers(BuildInfoItemUpdateSource.StateChanged);
	}

	private void NotifySubscribers(BuildInfoItemUpdateSource updateSource) {
		BuildInfo? current = _buildInfoHistory.CombinedInfo;
		if (current is null) return;
		current.CommentsCount = _commentsCount;
		var buildInfoItem = CreateNotification(current, updateSource);
		_subscribers.ForEach(s => s.Tell(buildInfoItem));
	}

	private void HandleDiscussion(BuildInfoHistory.Item buildInfoItem) =>
		Context.Parent.Tell(new ActorsApi.Discussions.BuildStatusChanged(_config!,
			new ActorsApi.BuildInfoItem(buildInfoItem.Info, _config!.Id,
				BuildInfoItemUpdateSource.StateChanged) {
				IsResolved = buildInfoItem.Resolved,
				Stats = buildInfoItem.Stats
			}));

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
