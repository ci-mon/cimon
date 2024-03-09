using Cimon.Jenkins;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Jenkins;

public class ClientFactory(IOptionsSnapshot<JenkinsSecrets> snapshot, 
	Func<JenkinsConfig, IJenkinsClient> factory)
{
	public IJenkinsClient Create(string connectorKey) {
		var secrets = snapshot.Get(connectorKey);
		var config = new JenkinsConfig {
			JenkinsUrl = secrets.Uri,
			Username = secrets.Login,
			ApiKey = secrets.Token
		};
		return factory(config);
	}
}
