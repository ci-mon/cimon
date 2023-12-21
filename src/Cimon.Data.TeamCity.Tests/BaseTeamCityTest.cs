using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cimon.Data.TeamCity.Tests;

public class BaseTeamCityTest
{
	protected ServiceProvider ServiceProvider = null!;

	[SetUp]
	public virtual void Setup() {
		var secrets = new TeamcitySecrets {
			Uri = new Uri("****"),
			Login = "****",
			Password = "****"
		};
		ServiceProvider = new ServiceCollection()
			.AddSingleton(Options.Create(secrets))
			.AddCimonDataTeamCity()
			.AddLogging()
			.BuildServiceProvider();
	}

	[TearDown]
	protected virtual void TearDown() => ServiceProvider?.Dispose();
}
