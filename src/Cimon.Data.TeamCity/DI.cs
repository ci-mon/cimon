﻿using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity;

public static class DI
{
	public static IServiceCollection AddCimonDataTeamCity(this IServiceCollection collection) {
		return collection.AddTransient<TcClientFactory, TcClientFactory>()
			.AddKeyedTransient<IBuildInfoProvider, TcBuildInfoProvider>(CISystem.TeamCity)
			.AddKeyedTransient<IBuildConfigProvider, TcBuildConfigProvider>(CISystem.TeamCity);
	}
}
