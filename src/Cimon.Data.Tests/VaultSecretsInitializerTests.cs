using Cimon.Data.Secrets;
using Cimon.Data.TeamCity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Tests;

[TestFixture]
public class VaultSecretsInitializerTests
{
	[Test]
	public void Configure() {
		var initializer = new VaultSecretsInitializer<CimonSecrets>(Options.Create(new VaultSecrets {
			Token = Guid.Empty.ToString(),
			Url = "http://localhost:8200",
			MountPoint = "infrastructure.cimon",
			Path = "dev"
		}), NullLogger<VaultSecretsInitializer<CimonSecrets>>.Instance);
		var secrets = Activator.CreateInstance<CimonSecrets>();
		initializer.Configure(secrets);
		secrets.BuildInfoMonitoring.SystemUserLogins.Should().Contain("user1");
	}
}
