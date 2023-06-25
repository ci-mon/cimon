using Cimon.Contracts;
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
		var result = await _provider.GetAll();
		result.Should().Contain(new BuildConfigInfo("app.my.test", null));
		result.Should().Contain(new BuildConfigInfo("app.my.multibranch", "master"));
		result.Should().Contain(new BuildConfigInfo("app.my.multibranch", "test2"));
	}

}