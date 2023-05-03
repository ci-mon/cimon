using Cimon.Data;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DI
{
	public static IServiceCollection AddCimonData(this IServiceCollection services) {
		return services
			.AddSingleton<IBuildMonitoringService, BuildMonitoringService>()
			.AddSingleton<BuildInfoService>()
			.AddSingleton<BuildDiscussionStoreService>()
			.AddSingleton<UserService>()
			.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList())
			.AddSingleton<MonitorService>();
	}
}
