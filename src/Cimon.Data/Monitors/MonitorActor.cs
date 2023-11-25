using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Actors;
using Cimon.Data.BuildInformation;
using Cimon.Data.Common;
using Cimon.DB.Models;
using ICancelable = Akka.Actor.ICancelable;

namespace Cimon.Data.Monitors;

class MonitorActor : ReceiveActor, IWithUnboundedStash
{
	record WatchedBuildInfo(BuildInMonitor Build, ReplaySubject<BuildInfo> Subject) : IBuildInfoStream
	{
		public BuildConfig BuildConfig => Build.BuildConfig;
		public IObservable<BuildInfo> BuildInfo => Subject;
	};

	private readonly ReplaySubject<MonitorData> _monitorSubject = new();
	private ImmutableDictionary<int, WatchedBuildInfo> _buildInfos =
		ImmutableDictionary<int, WatchedBuildInfo>.Empty;
	private IImmutableList<BuildInMonitor>? _builds;
	private readonly IObservable<MonitorData> _dataObservable;
	private volatile int _subscriptionsCount;
	private ICancelable _stopCountdown = Cancelable.CreateCanceled();

	public MonitorActor() {
		var scheduler = Context.System.Scheduler;
		var me = Self;
		_dataObservable = Observable.Create<MonitorData>(observer => {
			Interlocked.Increment(ref _subscriptionsCount);
			var subscription = _monitorSubject.Subscribe(observer);
			_stopCountdown.Cancel();
			return Disposable.Create(subscription, disposable => {
				disposable.Dispose();
				if (Interlocked.Decrement(ref _subscriptionsCount) == 0) {
					_stopCountdown =
						scheduler.ScheduleTellOnceCancelable(TimeSpan.FromSeconds(30), me, PoisonPill.Instance, me);
				}
			});
		});
		Receive<IObservable<MonitorModel>>(observable => Context.Observe(observable));
		Receive<MonitorModel>(model => {
			OnMonitorChange(model);
			Become(Ready);
		});
		ReceiveAny(_ => Stash!.Stash());
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
		foreach (var removed in diff.Removed) {
			var id = removed.BuildConfigId;
			Context.Parent.Tell(new BuildInfoServiceActorApi.Unsubscribe(id));
			toRemove.Add(id);
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
		_monitorSubject.OnNext(new MonitorData {
			Monitor = model,
			Builds = _buildInfos.Values
		});
	}

	private void Ready() {
		Receive<MonitorModel>(OnMonitorChange);
		Receive<ActorsApi.WatchMonitor>(_ => Sender.Tell(_dataObservable));
		Receive<BuildInfo>(info => {
			if (_buildInfos.TryGetValue(info.BuildConfigId, out var bucket)) {
				bucket.Subject.OnNext(info);
			}
		});
		Stash.UnstashAll();
	}

	public IStash Stash { get; set; }
}
