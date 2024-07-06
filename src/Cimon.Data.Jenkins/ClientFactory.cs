using Cimon.Jenkins;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Jenkins;

public class ClientFactory(IOptionsMonitor<JenkinsSecrets> options,
	Func<JenkinsConfig, IJenkinsClient> factory)
{
	public IJenkinsClient Create(string connectorKey, out JenkinsConfig config)  {
		var secrets = options.Get(connectorKey);
		config = new JenkinsConfig {
			JenkinsUrl = secrets.Uri,
			Username = secrets.Login,
			ApiKey = secrets.Token
		};
		return factory(config);
	}

	public IJenkinsClient Create(string connectorKey) => Create(connectorKey, out _);
}
