using Cimon.Data.Secrets;
using Cimon.Data.TeamCity;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Tests;

[TestFixture]
public class VaultSecretsInitializerTests
{
	[Test]
	public void Configure_NestedType() {
		var initializer = new VaultSecretsInitializer<CimonSecrets>(Options.Create(new VaultSecrets {
			Token = Guid.Empty.ToString(),
			Url = "http://localhost:8200",
			MountPoint = "infrastructure.cimon",
			Path = "dev"
		}), NullLogger<VaultSecretsInitializer<CimonSecrets>>.Instance, new HostingEnvironment());
		var secrets = Activator.CreateInstance<CimonSecrets>();
		initializer.Configure(secrets);
		secrets.BuildInfoMonitoring.SystemUserLogins.Should().Contain("user1");
	}
	[Test]
	public void Configure_Named() {
		var initializer = new VaultSecretsInitializer<TeamcitySecrets>(Options.Create(new VaultSecrets {
			Token = Guid.Empty.ToString(),
			Url = "http://localhost:8200",
			MountPoint = "infrastructure.cimon",
			Path = "dev"
		}), NullLogger<VaultSecretsInitializer<TeamcitySecrets>>.Instance, new HostingEnvironment());
		var secrets = Activator.CreateInstance<TeamcitySecrets>();
		initializer.Configure("teamcity_main", secrets);
		secrets.Uri.Should().Be("http://localhost:8112");
	}
}
