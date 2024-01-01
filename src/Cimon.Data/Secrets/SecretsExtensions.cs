using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Secrets;

public static class SecretsExtensions
{
	public static IServiceCollection ConfigureSecrets<TSecrets>(this IServiceCollection services) where TSecrets : class {
		ConfigureSecretsFromConfig<TSecrets>(services);
		services.Add(ServiceDescriptor.Transient(typeof(IConfigureOptions<TSecrets>),
			typeof(VaultSecretsInitializer<TSecrets>)));
		return services;
	}

	public static IServiceCollection ConfigureSecretsFromConfig<TSecrets>(this IServiceCollection services)
		where TSecrets : class {
		services.Add(ServiceDescriptor.Transient(typeof(IConfigureOptions<TSecrets>),
			typeof(ConfigurationSecretsInitializer<TSecrets>)));
		return services;
	}
}
