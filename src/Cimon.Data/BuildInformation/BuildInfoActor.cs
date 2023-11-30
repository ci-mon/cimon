﻿using System.Reactive.Linq;
using Akka.Actor;
using Akka.Event;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.ML;
using Cimon.DB.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.BuildInformation;

class BuildInfoActor : ReceiveActor
{
	private readonly int _buildConfigId;
	private readonly BuildConfigService _buildConfigService;
	private readonly BuildInfoMonitoringSettings _settings;
	private readonly IBuildFailurePredictor _buildFailurePredictor;

	record StopIfIdle;
	record GetBuildInfo;

	private readonly GetBuildInfo _getBuildInfo = new();
	private readonly StopIfIdle _stopIfIdle = new();
	private readonly List<IActorRef> _subscribers = new();
	private BuildConfigModel? _config;
	private IBuildInfoProvider? _provider;
	private ICancelable? _refreshBuildInfoScheduler;
	private readonly HashSet<string> _systemUserLogins;
	private readonly RingBuffer<BuildInfo> _buildInfos = new(50);
	private int _commentsCount;
	private bool _discussionWatched;
	private bool _discussionOpen;
	private readonly ILoggingAdapter _log = Context.GetLogger();
	private readonly IServiceScope _scope;
	private CIConnectorInfo _connectorInfo;

	public BuildInfoActor(int buildConfigId, IServiceProvider serviceProvider, BuildConfigService buildConfigService,
			BuildInfoMonitoringSettings settings, IBuildFailurePredictor buildFailurePredictor) {
		_scope = serviceProvider.CreateScope();
		_buildConfigId = buildConfigId;
		_buildConfigService = buildConfigService;
		_settings = settings;
		_buildFailurePredictor = buildFailurePredictor;
		_systemUserLogins = new HashSet<string>(settings.SystemUserLogins, StringComparer.OrdinalIgnoreCase);
		Receive<BuildInfoServiceActorApi.Subscribe>(OnSubscribe);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(Unsubscribe);
		Receive<StopIfIdle>(OnStopIfIdle);
		ReceiveAsync<BuildConfigModel>(InitBuildConfig);
		ReceiveAsync<GetBuildInfo>(OnGetBuildInfo);
		Receive<BuildInfo>(HandleBuildInfo);
		Receive<Terminated>(_ => {
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
		if (_buildInfos.Last is {} current) {
			Sender.Tell(CreateNotification(current));
		}
	}

	private BuildInfoServiceActorApi.BuildInfoItem CreateNotification(BuildInfo current) {
		return new BuildInfoServiceActorApi.BuildInfoItem(current, _config!.Id);
	}

	private async Task InitBuildConfig(BuildConfigModel config) {
		_config = config;
		_provider = _scope.ServiceProvider.GetRequiredKeyedService<IBuildInfoProvider>(config.Connector.CISystem);
		_connectorInfo = await _buildConfigService.GetConnectorInfo(config.Connector);
		_refreshBuildInfoScheduler ??= Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
			TimeSpan.Zero, _settings.Delay, Self, _getBuildInfo, Self);
	}

	private void HandleBuildInfo(BuildInfo? newInfo) {
		if (newInfo is null) return;
		if (_buildInfos.Last?.Number.Equals(newInfo.Number) is true) {
			return;
		}
		newInfo.Changes = newInfo.Changes.Where(x => !_systemUserLogins.Contains(x.Author.Name)).ToList();
		newInfo.FailureSuspect = _buildFailurePredictor.FindFailureSuspect(newInfo);
		HandleDiscussion(newInfo);
		_buildInfos.Add(newInfo);
		NotifySubscribers(newInfo);
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

	private async Task OnGetBuildInfo(GetBuildInfo _) {
		if (_config!.DemoState is not null) {
			_config.DemoState.Duration = TimeSpan.FromMinutes(Random.Shared.Next(120));
			Self.Tell(_config.DemoState);
			return;
		}
		try {
			var options = new BuildInfoQueryOptions {
				LastBuildNumber = _buildInfos.Last?.Number
			};
			var query = new BuildInfoQuery(_connectorInfo, _config!, options);
			var info = await _provider!.FindInfo(query);
			Self.Tell(info ?? BuildInfo.NoData);
		} catch (Exception e) {
			_log.Error(e, e.Message);
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
