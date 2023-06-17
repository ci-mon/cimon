﻿using Cimon.Contracts.Services;
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
			.AddSingleton<IBuildMonitoringService, BuildMonitoringService>()
			.AddSingleton<BuildInfoService>()
			.AddSingleton<BuildDiscussionStoreService>()
			.AddSingleton<UserListService>()
			.AddSingleton<MentionsService>()
			.AddSingleton<ITechnicalUsers, TechnicalUsers>()
			.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>()
			.AddSingleton<BuildConfigService>()
			.AddSingleton<IList<IBuildInfoProvider>>(sp => sp.GetServices<IBuildInfoProvider>().ToList())
			.AddSingleton<MonitorService>()
			.AddMediatR(configuration => {
				configuration.RegisterServicesFromAssemblyContaining<AddCommentNotificationHandler>();
			});
	}
}