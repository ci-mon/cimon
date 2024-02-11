using Cimon.Contracts.CI;
using Cimon.Data.Tests;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity.Tests;

public class BaseTeamCityTest() : BaseCIConnectorTest<TeamcitySecrets, 
	TcBuildInfoProvider, TcBuildConfigProvider>("teamcity_main", CISystem.TeamCity)
{
	protected override void SetupDI(IServiceCollection serviceCollection) {
		serviceCollection.AddCimonDataTeamCity();
	}
}
