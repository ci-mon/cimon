using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data.Secrets;
using Cimon.Data.TeamCity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Tests;

using Cimon.Data.ML;
using Microsoft.FeatureManagement;

public abstract class BaseCIConnectorTest<TSecrets, TBuildInfoProvider, TBuildConfigProvider>(string key, CISystem ciSystem) 
	where TSecrets : class 
	where TBuildConfigProvider : class, IBuildConfigProvider
	where TBuildInfoProvider : class, IBuildInfoProvider
{
	protected ServiceProvider ServiceProvider = null!;
	protected CIConnectorInfo DefaultConnector { get; set; } = new(key, new Dictionary<string, string>());
	protected TBuildConfigProvider BuildConfigProvider { get; private set; } = null!;
	protected TBuildInfoProvider BuildInfoProvider { get; private set; } = null!;

	protected abstract void SetupDI(IServiceCollection serviceCollection);

	[SetUp]
	protected virtual void Setup() {
		var config = new ConfigurationBuilder().AddUserSecrets("0574c095-3b5d-4b4a-83a0-60bd33381798").Build();
		var serviceCollection = new ServiceCollection()
			.AddSingleton<IConfiguration>(config)
			.ConfigureSecretsFromConfig<TSecrets>()
			.AddLogging();
		serviceCollection.AddCimonML().AddFeatureManagement();
		SetupDI(serviceCollection);
		ServiceProvider = serviceCollection.BuildServiceProvider();
		BuildConfigProvider = ServiceProvider.GetRequiredKeyedService<IBuildConfigProvider>(ciSystem) as TBuildConfigProvider;
		BuildInfoProvider = (ServiceProvider.GetRequiredKeyedService<IBuildInfoProvider>(ciSystem) as TBuildInfoProvider)!;
	}

	[TearDown]
	protected virtual void TearDown() => ServiceProvider?.Dispose();
}
