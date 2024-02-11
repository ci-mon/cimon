using Microsoft.Extensions.Options;
using Narochno.Jenkins;

namespace Cimon.Data.Jenkins;

public class ClientFactory(IOptionsSnapshot<JenkinsSecrets> snapshot)
{

	public JenkinsClient Create(string connectorKey) {
		var secrets = snapshot.Get(connectorKey);
		var config = new JenkinsConfig {
			JenkinsUrl = secrets.Uri.ToString(),
			Username = secrets.Login,
			ApiKey = secrets.Token
		};
		return new JenkinsClient(config);
	}
}
