using Microsoft.Extensions.Options;

namespace Cimon.Data.Jenkins.Tests;

public class BaseJenkinsTest
{
	protected ClientFactory Factory = null!;

	[SetUp]
	protected virtual void Setup() {
		var secrets = new JenkinsSecrets {
			Uri = new Uri("http://localhost:8080"),
			Login = "admin",
			Token = "11338fd16b8c7c51052d933d9f265ce528"
		};
		Factory = new ClientFactory(Options.Create(secrets));
	}
}