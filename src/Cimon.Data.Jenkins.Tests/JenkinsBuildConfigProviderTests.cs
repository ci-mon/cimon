using Cimon.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Jenkins.Tests;

public class JenkinsBuildConfigProviderTests
{
	private ClientFactory _factory;
	private JenkinsBuildConfigProvider _provider;

	[SetUp]
	public void Setup() {
		var secrets = new JenkinsSecrets {
			Uri = new Uri("http://localhost:8080"),
			Login = "admin",
			Token = "11338fd16b8c7c51052d933d9f265ce528"
		};
		_factory = new ClientFactory(Options.Create(secrets));
		_provider = new JenkinsBuildConfigProvider(_factory);
	}

	[Test]
	public async Task GetAll() {
		var result = await _provider.GetAll();
		result.Should().HaveCount(1).And.Contain(new BuildConfigInfo("app.my.test"));
	}

	[Test]
	public void x() {
		var x = _factory.Create().Views().FirstOrDefault();
	}
}