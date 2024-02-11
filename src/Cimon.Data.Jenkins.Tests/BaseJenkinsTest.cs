using Cimon.Contracts.CI;
using Cimon.Data.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Jenkins.Tests;

public class BaseJenkinsTest() : BaseCIConnectorTest<JenkinsSecrets, JenkinsBuildInfoProvider,
	JenkinsBuildConfigProvider>("jenkins_main", CISystem.Jenkins)
{
	protected ClientFactory Factory { get; private set; }

	protected override void SetupDI(IServiceCollection serviceCollection) {
		serviceCollection.AddCimonDataJenkins();
	}

	protected override void Setup() {
		base.Setup();
		Factory = ServiceProvider.GetRequiredService<ClientFactory>();
	}
}
