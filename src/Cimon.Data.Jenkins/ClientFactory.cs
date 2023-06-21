using Microsoft.Extensions.Options;
using Narochno.Jenkins;

namespace Cimon.Data.Jenkins;

public class ClientFactory
{
	private readonly JenkinsSecrets _options;
	public ClientFactory(IOptions<JenkinsSecrets> options) {
		_options = options.Value;
	}

	public JenkinsClient Create() {
		var config = new JenkinsConfig
		{
			JenkinsUrl = _options.Uri.ToString(),
			Username = _options.Login,
			ApiKey = _options.Token
		};
		return new JenkinsClient(config);
	}
}