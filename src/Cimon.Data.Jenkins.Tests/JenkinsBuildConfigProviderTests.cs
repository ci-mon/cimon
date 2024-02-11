using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using FluentAssertions;

namespace Cimon.Data.Jenkins.Tests;

public class JenkinsBuildConfigProviderTests : BaseJenkinsTest
{

	[Test]
	public async Task GetAll() {
		var result = await BuildConfigProvider.GetAll(DefaultConnector);
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
