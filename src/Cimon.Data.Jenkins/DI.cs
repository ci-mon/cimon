using Cimon.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Jenkins;

public static class DI
{
	public static IServiceCollection AddCimonDataJenkins(this IServiceCollection collection) {
		return collection.AddTransient<ClientFactory>()
			.AddTransient<IBuildConfigProvider, JenkinsBuildConfigProvider>()
			.AddTransient<IBuildInfoProvider, JenkinsBuildInfoProvider>();
	}
}