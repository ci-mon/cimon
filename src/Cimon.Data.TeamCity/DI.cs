using Cimon.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity;

public static class DI
{
	public static IServiceCollection AddCimonDataTeamCity(this IServiceCollection collection) {
		return collection.AddTransient<TcClientFactory, TcClientFactory>()
			.AddTransient<IBuildInfoProvider, TcBuildInfoProvider>()
			.AddTransient<IBuildConfigProvider, TcBuildConfigProvider>();
	}
}