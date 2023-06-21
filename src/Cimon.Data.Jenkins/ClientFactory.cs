using JenkinsNetClient;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Jenkins;

public class ClientFactory
{
	private readonly JenkinsSecrets _options;
	public ClientFactory(IOptions<JenkinsSecrets> options) {
		_options = options.Value;
	}

	public JenkinsServer Create() {
		JenkinsConnection myConn = new JenkinsConnection(_options.Uri.ToString(), _options.Login, _options.Token);
		return new JenkinsServer(myConn);
	}
}