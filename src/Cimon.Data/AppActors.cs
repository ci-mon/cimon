using System.Collections.Immutable;
using System.Reactive.Linq;
using Akka.Actor;
using Akka.DependencyInjection;
using Cimon.Data.BuildInformation;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;
using Cimon.Data.Users;
using Cimon.DB.Models;
using Optional;
using User = Cimon.Contracts.User;

namespace Cimon.Data;

using System.Diagnostics;

public class AppActors
{
	private ActorSystem _actorSystem = null!;
	public static AppActors Instance { get; set; }

	public IActorRef MonitorService { get; set; }
	public IActorRef BuildInfoService { get; set; }
	public IActorRef DiscussionStore { get; set; }
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
		DiscussionStore = _actorSystem.ActorOf(_actorSystem.DI().Props<DiscussionStoreActor>(MentionsMonitor),
			nameof(DiscussionStore));
		BuildInfoService = _actorSystem.ActorOf(_actorSystem.DI().Props<BuildInfoServiceActor>(DiscussionStore),
			nameof(BuildInfoService));
		MonitorService = _actorSystem.ActorOf(_actorSystem.DI().Props<MonitorServiceActor>(BuildInfoService),
			nameof(MonitorService));
	}

	public static Task<IObservable<IImmutableList<MentionInfo>>> GetMentions(User user) {
		string name = user.Name.Name;
		bool nameIsEmpty = string.IsNullOrWhiteSpace(name);
		Debug.Assert(!nameIsEmpty, "nameIsEmpty", user?.ToString());
		if (nameIsEmpty) {
			return Task.FromResult(Observable.Empty<IImmutableList<MentionInfo>>());
		}
		return Instance.UserSupervisor.Ask(new ActorsApi.GetUserMentions(name));
	}
}

public record MentionInBuildConfig(MentionInfo Mention, Option<BuildConfigModel> BuildConfig);
