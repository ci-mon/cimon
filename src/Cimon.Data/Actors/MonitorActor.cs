using System.Collections.Immutable;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.Common;
using Cimon.Data.Monitors;
using Cimon.DB.Models;
using ICancelable = Akka.Actor.ICancelable;

namespace Cimon.Data.Actors;

class MonitorActor : ReceiveActor, IWithUnboundedStash
{
	record WatchedBuildInfo(BuildInMonitor Build, ReplaySubject<BuildInfo> Subject) : IBuildInfoStream
	{
		public BuildConfig BuildConfig => Build.BuildConfig;
		public IObservable<BuildInfo> BuildInfo => Subject;
	};

	private readonly ReplaySubject<MonitorData> _monitorSubject = new();
	private ImmutableDictionary<string, WatchedBuildInfo> _buildInfos =
		ImmutableDictionary<string, WatchedBuildInfo>.Empty;
	private MonitorModel? _model;
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
		if (_model is null) return;
		foreach (var build in _model.Builds) {
			var id = build.BuildConfigId.ToString();
			Context.Parent.Tell(new BuildInfoServiceActorApi.Unsubscribe(id));
		}
	}

	private void OnMonitorChange(MonitorModel model) {
		var builds = _model?.Builds;
		var diff = builds.CompareWith(model.Builds, build => build.BuildConfigId);
		var toRemove = new List<string>();
		foreach (var removed in diff.Removed) {
			var id = removed.BuildConfigId.ToString();
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
			.AddRange(toAdd.Select(x => new KeyValuePair<string, WatchedBuildInfo>(x.BuildConfig.Id.ToString(), x)));
		_model = model;
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
