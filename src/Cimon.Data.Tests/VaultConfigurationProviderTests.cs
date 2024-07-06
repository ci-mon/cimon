using Cimon.Data.Secrets;
using Cimon.Data.TeamCity;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Tests;

[TestFixture]
public class VaultConfigurationProviderTests
{
	[Test]
	public void Configure_NestedType() {
		var initializer = new VaultConfigurationProvider("Secrets", new VaultSecrets {
			Token = Guid.Empty.ToString(),
			Url = "http://localhost:8200",
			MountPoint = "infrastructure.cimon",
			Path = "dev"
		}, new HostingEnvironment(), NullLogger<VaultConfigurationProvider>.Instance);
		var secrets = Activator.CreateInstance<CimonSecrets>();
		initializer.Load();
		secrets.BuildInfoMonitoring.SystemUserLogins.Should().Contain("user1");
	}
}
