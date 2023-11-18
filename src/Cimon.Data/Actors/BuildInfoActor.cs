using Akka.Actor;
using Akka.Event;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.DB.Models;

namespace Cimon.Data.Actors;

class BuildInfoActor : ReceiveActor
{
	record StopIfIdle;
	record GetBuildInfo;

	private readonly GetBuildInfo _getBuildInfo = new();
	private readonly StopIfIdle _stopIfIdle = new();
	private readonly List<IActorRef> _subscribers = new();
	private BuildConfig? _config;
	private IBuildInfoProvider? _provider;
	private ICancelable? _refreshBuildInfoScheduler;

	public BuildInfoActor(IEnumerable<IBuildInfoProvider> buildInfoProviders) {
		Receive<BuildInfoServiceActorApi.Unsubscribe>(_ => {
			_subscribers.Remove(Sender);
			if (_subscribers.Count == 0)
				Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), Self, _stopIfIdle, Self);
		});
		Receive<StopIfIdle>(_ => {
			if (_subscribers.Count == 0) {
				_refreshBuildInfoScheduler?.Cancel();
				Self.Tell(PoisonPill.Instance);
			}
		});
		Receive<BuildConfig>(config => {
			_subscribers.Add(Sender);
			if (_config == null) {
				_config = config;
				_provider = buildInfoProviders.Single(p => p.CiSystem == config.CISystem);
				_refreshBuildInfoScheduler = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(
					TimeSpan.FromSeconds(Random.Shared.Next(3,10)), TimeSpan.FromSeconds(5), Self,
					_getBuildInfo, Self);
			}
		});
		Receive<GetBuildInfo>(_ => {
			var options = new BuildInfoQueryOptions();
			var query = new BuildInfoQuery(_config!, options);
			if (_config!.DemoState is not null) {
				_config.DemoState.BuildConfigId = _config.Id.ToString();
				_config.DemoState.Duration = TimeSpan.FromMinutes(Random.Shared.Next(120));
				Self.Tell(_config.DemoState);
				return;
			}
			_provider!.FindInfo(query).PipeTo(Self);
		});
		Receive<BuildInfo>(info => {
			if (info is null) return;
			_subscribers.ForEach(s=>s.Tell(info));
		});
		Receive<Status.Failure>(failure => {
			Context.GetLogger().Error(failure.Cause, failure.Cause.Message);
		});
	}
}
