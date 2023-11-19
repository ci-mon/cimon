﻿using Akka.Actor;
using Akka.DependencyInjection;
using Cimon.Data.Actors;

namespace Cimon.Data;

public class AppActors
{
	private ActorSystem _actorSystem;
	public static AppActors Instance { get; set; }

	public IActorRef MonitorService { get; set; }
	public IActorRef BuildInfoService { get; set; }
	public IActorRef DiscussionsService { get; set; }

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
		DiscussionsService = _actorSystem.ActorOf(_actorSystem.DI().Props<DiscussionStoreActor>());
		BuildInfoService = _actorSystem.ActorOf(_actorSystem.DI().Props<BuildInfoServiceActor>(DiscussionsService));
		MonitorService = _actorSystem.ActorOf(_actorSystem.DI().Props<MonitorServiceActor>(BuildInfoService));
	}
}