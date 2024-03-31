using Cimon.Contracts;
using Cimon.Contracts.AppFeatures;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.CIConnectors;
using Cimon.Data.DemoData;
using Cimon.Data.Discussions;
using Cimon.Data.Features;
using Cimon.Data.Monitors;
using Cimon.Data.Users;
using Microsoft.Extensions.Hosting;
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
		return serviceCollection;
	}

	public static MediatRServiceConfiguration AddCimonData(this MediatRServiceConfiguration configuration) {
		return configuration.RegisterServicesFromAssemblyContaining<AddReplyCommentNotificationHandler>();
	}
}
