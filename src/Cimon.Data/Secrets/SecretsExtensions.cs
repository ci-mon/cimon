using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Secrets;

public static class SecretsExtensions
{
	public static IServiceCollection ConfigureSecrets<TSecrets>(this IServiceCollection services, bool isDevelopment) where TSecrets : class {
		services.Add(ServiceDescriptor.Transient(typeof(IConfigureOptions<TSecrets>),
			typeof(VaultSecretsInitializer<TSecrets>)));
		if (isDevelopment) {
			ConfigureUserSecrets<TSecrets>(services);
		}
		return services;
	}

	public static IServiceCollection ConfigureUserSecrets<TSecrets>(this IServiceCollection services)
		where TSecrets : class {
		services.Add(ServiceDescriptor.Transient(typeof(IConfigureOptions<TSecrets>),
			typeof(LocalSecretsInitializer<TSecrets>)));
		return services;
	}
}
