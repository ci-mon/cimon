using Cimon.Contracts.Services;
using Cimon.Data.BuildInformation;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;
using Cimon.Data.Users;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DI
{
	public static IServiceCollection AddCimonData(this IServiceCollection services) {
		return services
			.AddSingleton<UserManager>()
			.AddSingleton<LdapClient>()
			.AddSingleton<ITechnicalUsers>(x => x.GetRequiredService<UserManager>())
			.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>()
			.AddSingleton<BuildConfigService>()
			.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList())
			.AddSingleton<MonitorService>();
	}

	public static MediatRServiceConfiguration AddCimonData(this MediatRServiceConfiguration configuration) {
		return configuration.RegisterServicesFromAssemblyContaining<AddReplyCommentNotificationHandler>();
	}
}
