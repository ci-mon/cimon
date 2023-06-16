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
		var initializer = new VaultSecretsInitializer<TeamCitySecrets>(Options.Create(new VaultSettings {
			Token = Guid.Empty.ToString(),
			Url = "http://localhost:8200",
			MountPoint = "infrastructure.cimon",
			Path = "dev"
		}), NullLogger<VaultSecretsInitializer<TeamCitySecrets>>.Instance);
		var secrets = Activator.CreateInstance<TeamCitySecrets>();
		initializer.Configure(secrets);
		secrets.Login.Should().Be("admin");
		secrets.Password.Should().Be("admin");
		secrets.Uri.Should().NotBeNull();
	}
}