using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using FluentAssertions;

namespace Cimon.Data.Jenkins.Tests;

public class JenkinsBuildConfigProviderTests : BaseJenkinsTest
{
	private JenkinsBuildConfigProvider _provider = null!;

	protected override void Setup() {
		base.Setup();
		_provider = new JenkinsBuildConfigProvider(Factory);
	}

	[Test]
	public async Task GetAll() {
		var result = await _provider.GetAll(new CIConnectorInfo("main", new Dictionary<string, string>()));
		result.Should().Contain(new BuildConfig() {
			Key = "app.my.multibranch",
			Branch = "master"
		});
		result.Should().Contain(new BuildConfig() {
			Key = "app.my.multibranch",
			Branch = "test2"
		});
	}

}
