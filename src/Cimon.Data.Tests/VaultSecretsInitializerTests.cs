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
		var initializer = new VaultSecretsInitializer<TeamcitySecrets>(Options.Create(new VaultSettings {
			Token = Guid.Empty.ToString(),
			Url = "http://localhost:8200",
			MountPoint = "infrastructure.cimon",
			Path = "dev"
		}), NullLogger<VaultSecretsInitializer<TeamcitySecrets>>.Instance);
		var secrets = Activator.CreateInstance<TeamcitySecrets>();
		initializer.Configure(secrets);
		secrets.Login.Should().Be("admin");
		secrets.Password.Should().Be("admin");
		secrets.Uri.Should().NotBeNull();
	}
}