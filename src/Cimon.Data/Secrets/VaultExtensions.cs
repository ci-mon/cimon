using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Secrets;

public static class VaultExtensions
{
	public static IServiceCollection ConfigureVaultSecrets<TSecrets>(this IServiceCollection services) where TSecrets : class {
		services.Add(ServiceDescriptor.Transient(typeof(IConfigureOptions<TSecrets>),
			typeof(VaultSecretsInitializer<TSecrets>)));
		return services;
	}
}