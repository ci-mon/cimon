﻿using Cimon.Data;
using Cimon.Data.Discussions;
using Cimon.Data.Users;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class DI
{
	public static IServiceCollection AddCimonData(this IServiceCollection services) {
		return services
			.AddSingleton<IBuildMonitoringService, BuildMonitoringService>()
			.AddSingleton<BuildInfoService>()
			.AddSingleton<BuildDiscussionStoreService>()
			.AddSingleton<UserListService>()
			.AddSingleton<MentionsService>()
			.AddSingleton<ITechnicalUsers, TechnicalUsers>()
			.AddScoped<ICurrentUserAccessor, CurrentCurrentUserAccessor>()
			.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList())
			.AddSingleton<MonitorService>();
	}
}