using Akka.DependencyInjection;
using Akka.Hosting;
using Cimon.Contracts;
using Cimon.Contracts.AppFeatures;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.BuildInformation;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.DemoData;
using Cimon.Data.Discussions;
using Cimon.Data.Features;
using Cimon.Data.Monitors;
using Cimon.Data.Users;
using Microsoft.FeatureManagement;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DI
{
	public static IServiceCollection AddCimonData(this IServiceCollection services) {
		var serviceCollection = services
			.AddSingleton<UserManager>()
			.AddSingleton<LdapClient>()
			.AddSingleton<ITechnicalUsers>(x => x.GetRequiredService<UserManager>())
			.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>()
			.AddSingleton<BuildConfigService>()
			.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList())
			.AddSingleton<MonitorService>()
			.AddScoped<IUserNameProvider, UserNameProvider>()
			.AddKeyedTransient<IBuildInfoProvider, DemoBuildInfoProvider>(CISystem.Demo)
			.AddSingleton<AppFeatureManager>()
			.AddSingleton<IAppInitializer>(sp => sp.GetRequiredService<AppFeatureManager>());
		serviceCollection
			.AddSingleton<IFeatureDefinitionProvider>(sp => sp.GetRequiredService<AppFeatureManager>())
			.AddScopedFeatureManagement()
			.WithTargeting<TargetingContextAccessor>();
		serviceCollection.AddAkka("cimon", (builder, serviceProvider) => {
			builder.AddSetup(DependencyResolverSetup.Create(serviceProvider))
				.ConfigureLoggers(x => x.AddLoggerFactory())
				.WithActors((system, registry) => {
					var userSupervisor = system.ActorOf(system.DI().Props<UserSupervisorActor>(), "UserSupervisor");
					var mentionsMonitor = system.ActorOf(system.DI().Props<MentionsMonitorActor>(userSupervisor),
						"MentionsMonitor");
					var discussionStore = system.ActorOf(system.DI().Props<DiscussionStoreActor>(mentionsMonitor),
						"DiscussionStore");
					var buildInfoService = system.ActorOf(system.DI().Props<BuildInfoServiceActor>(discussionStore),
						"BuildInfoService");
					registry.Register<UserSupervisorActor>(userSupervisor);
					registry.Register<MentionsMonitorActor>(mentionsMonitor);
					registry.Register<DiscussionStoreActor>(discussionStore);
					registry.Register<BuildInfoServiceActor>(buildInfoService);
					registry.Register<MonitorServiceActor>(
						system.ActorOf(system.DI().Props<MonitorServiceActor>(buildInfoService), "MonitorService"));
				});
		});
		return serviceCollection;
	}

	public static MediatRServiceConfiguration AddCimonData(this MediatRServiceConfiguration configuration) {
		return configuration.RegisterServicesFromAssemblyContaining<AddReplyCommentNotificationHandler>();
	}
}
