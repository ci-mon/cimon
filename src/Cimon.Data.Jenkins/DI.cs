using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Jenkins;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Jenkins;

public static class DI
{
	public static IServiceCollection AddCimonDataJenkins(this IServiceCollection collection) {
		return collection.AddTransient<ClientFactory>()
			.AddKeyedTransient<IBuildConfigProvider, JenkinsBuildConfigProvider>(CISystem.Jenkins)
			.AddKeyedTransient<IBuildInfoProvider, JenkinsBuildInfoProvider>(CISystem.Jenkins)
			.AddJenkinsClientFactory();
	}
}
