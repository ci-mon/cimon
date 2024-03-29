﻿using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.BuildInformation;
using Cimon.Data.Common;
using Cimon.DB.Models;
using ICancelable = Akka.Actor.ICancelable;

namespace Cimon.Data.Monitors;

class MonitorActor : ReceiveActor, IWithUnboundedStash
{
	record WatchedBuildInfo(BuildInMonitor Build, ReplaySubject<BuildInfo> Subject) 
		: IBuildInfoStream, IBuildInfoSnapshot
	{
		public BuildConfigModel BuildConfig => Build.BuildConfig;
		public IObservable<BuildInfo> BuildInfo => Subject;
		public BuildInfo? LatestInfo { get; set; }
	};

	private readonly ReplaySubject<MonitorData> _monitorSubject = new(1);
	private ImmutableDictionary<int, WatchedBuildInfo> _buildInfos =
		ImmutableDictionary<int, WatchedBuildInfo>.Empty;
	private IImmutableList<BuildInMonitor>? _builds;
	private readonly IObservable<MonitorData> _dataObservable;
	private volatile int _subscriptionsCount;
	private ICancelable _stopCountdown = Cancelable.CreateCanceled();
	private readonly List<IActorRef> _watchers = new();
	private MonitorModel _model;

	public MonitorActor() {
		var scheduler = Context.System.Scheduler;
		var me = Self;
		_dataObservable = Observable.Create<MonitorData>(observer => {
			Interlocked.Increment(ref _subscriptionsCount);
			var subscription = _monitorSubject.Subscribe(observer);
			_stopCountdown.Cancel();
			return Disposable.Create(subscription, disposable => {
				disposable.Dispose();
				OnWatcherRemoved(scheduler, me);
			});
		});
		Receive<IObservable<MonitorModel>>(observable => Context.Observe(observable));
		Receive<MonitorModel>(model => {
			OnMonitorChange(model);
			Become(Ready);
		});
		ReceiveAny(_ => Stash!.Stash());
	}

	private void OnWatcherRemoved(IScheduler scheduler, IActorRef me) {
		if (Interlocked.Decrement(ref _subscriptionsCount) == 0) {
			var shutdownDelay = TimeSpan.FromSeconds(30);
			_stopCountdown = scheduler.ScheduleTellOnceCancelable(shutdownDelay, me, PoisonPill.Instance, me);
		}
	}

	protected override void PostStop() {
		base.PostStop();
		if (_builds is null) return;
		foreach (var build in _builds) {
			Context.Parent.Tell(new BuildInfoServiceActorApi.Unsubscribe(build.BuildConfigId));
		}
	}

	private void OnMonitorChange(MonitorModel model) {
		var diff = _builds.CompareWith(model.Builds, build => build.BuildConfigId);
		var toRemove = new List<int>();
		foreach (var monitor in diff.Removed) {
			Context.Parent.Tell(new BuildInfoServiceActorApi.Unsubscribe(monitor.BuildConfigId));
			toRemove.Add(monitor.BuildConfigId);
		}
		var toAdd = new List<WatchedBuildInfo>();
		foreach (var added in diff.Added) {
			Context.Parent.Tell(new BuildInfoServiceActorApi.Subscribe(added.BuildConfig));
			var buildInfoData = new WatchedBuildInfo(added, new ReplaySubject<BuildInfo>(1));
			toAdd.Add(buildInfoData);
		}
		_buildInfos = _buildInfos
			.RemoveRange(toRemove)
			.AddRange(toAdd.Select(x => new KeyValuePair<int, WatchedBuildInfo>(x.BuildConfig.Id, x)));
		_builds = model.Builds.ToImmutableList();
		_model = model;
		_monitorSubject.OnNext(new MonitorData {
			Monitor = model,
			Builds = _buildInfos.Values
		});
	}

	private void Ready() {
		Receive<MonitorModel>(OnMonitorChange);
		Receive<ActorsApi.WatchMonitor>(_ => Sender.Tell(_dataObservable));
		Receive<ActorsApi.UnWatchMonitorByActor>(_ => {
			OnWatcherRemoved(Context.System.Scheduler, Self);
			_watchers.Remove(Sender);
		});
		Receive<ActorsApi.WatchMonitorByActor>(_ => {
			Interlocked.Increment(ref _subscriptionsCount);
			Context.Watch(Sender);
			_watchers.Add(Sender);
			if (_buildInfos.Count > 0) {
				Sender.Tell(new ActorsApi.MonitorInfo(_model, _buildInfos.Values));
			}
		});
		Receive<Terminated>(msg => {
			OnWatcherRemoved(Context.System.Scheduler, Self);
			_watchers.Remove(msg.ActorRef);
		});
		Receive<ActorsApi.BuildInfoItem>(info => {
			if (_buildInfos.TryGetValue(info.BuildConfigId, out var bucket)) {
				bucket.Subject.OnNext(info.BuildInfo);
				bucket.LatestInfo = info.BuildInfo;
			}
			foreach (var watcher in _watchers) {
				watcher.Tell(new ActorsApi.MonitorInfo(_model, _buildInfos.Values));
			}
		});
		Receive<ActorsApi.RefreshMonitor>(Refresh);
		Stash.UnstashAll();
	}

	private void Refresh(ActorsApi.RefreshMonitor obj) {
		foreach (var build in _builds!) {
			Context.Parent.Tell(new BuildInfoServiceActorApi.Refresh(build.BuildConfig));
		}
	}

	public IStash Stash { get; set; }
}
