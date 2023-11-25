using System.Collections.Immutable;
using Akka.Actor;
using Akka.DependencyInjection;
using Cimon.Contracts;
using Cimon.Data.Actors;
using Cimon.Data.BuildInformation;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;
using Cimon.Data.Users;

namespace Cimon.Data;

public class AppActors
{
	private ActorSystem _actorSystem;
	public static AppActors Instance { get; set; }

	public IActorRef MonitorService { get; set; }
	public IActorRef BuildInfoService { get; set; }
	public IActorRef DiscussionsService { get; set; }
	public IActorRef UserSupervisor { get; set; }
	public IActorRef MentionsMonitor { get; set; }

	public static void Init(IServiceProvider serviceProvider) {
		var instance = new AppActors();
		instance.InitInternal(serviceProvider);
		Instance = instance;
	}

	private void InitInternal(IServiceProvider serviceProvider) {
		var bootstrap = BootstrapSetup.Create();
		var di = DependencyResolverSetup.Create(serviceProvider);
		var actorSystemSetup = bootstrap.And(di);
		_actorSystem = ActorSystem.Create("cimon", actorSystemSetup);
		UserSupervisor = _actorSystem.ActorOf(_actorSystem.DI().Props<UserSupervisorActor>(), nameof(UserSupervisor));
		MentionsMonitor = _actorSystem.ActorOf(_actorSystem.DI().Props<MentionsMonitorActor>(UserSupervisor),
			nameof(MentionsMonitor));
		DiscussionsService = _actorSystem.ActorOf(_actorSystem.DI().Props<DiscussionStoreActor>(MentionsMonitor),
			nameof(DiscussionsService));
		BuildInfoService = _actorSystem.ActorOf(_actorSystem.DI().Props<BuildInfoServiceActor>(DiscussionsService),
			nameof(BuildInfoService));
		MonitorService = _actorSystem.ActorOf(_actorSystem.DI().Props<MonitorServiceActor>(BuildInfoService),
			nameof(MonitorService));
	}

	public static Task<IObservable<IImmutableList<MentionInfo>>> GetMentions(User user) {
		return Instance.UserSupervisor.Ask(new ActorsApi.GetUserMentions(user.Name.Name));
	}
}
