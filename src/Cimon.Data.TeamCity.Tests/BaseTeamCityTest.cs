using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cimon.Data.TeamCity.Tests;

using Cimon.Contracts.Services;
using Cimon.Data.Secrets;
using Microsoft.Extensions.Configuration;

public class BaseTeamCityTest
{
	protected ServiceProvider ServiceProvider = null!;

	protected CIConnectorInfo DefaultConnector { get; set; } = new("teamcity_main", new Dictionary<string, string>());

	[SetUp]
	public virtual void Setup() {
		var config = new ConfigurationBuilder().AddUserSecrets("0574c095-3b5d-4b4a-83a0-60bd33381798").Build();
		ServiceProvider = new ServiceCollection()
			.AddSingleton<IConfiguration>(config)
			.ConfigureUserSecrets<TeamcitySecrets>()
			.AddCimonDataTeamCity()
			.AddLogging()
			.BuildServiceProvider();
	}

	[TearDown]
	protected virtual void TearDown() => ServiceProvider?.Dispose();
}
