using System.DirectoryServices.Protocols;
using System.Net;
using Cimon.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Users;

public class LdapClientSecrets
{
	public string Host { get; set; } = "dc.com";
	public TimeSpan ConnectionTimeout { get; set; }
}

public class LdapClient
{
	private readonly ILogger _logger;
	private readonly LdapClientSecrets _options;

	public LdapClient(ILogger<LdapClient> logger, IOptions<LdapClientSecrets> options) {
		_logger = logger;
		_options = options.Value;
	}

	public async Task<bool> FindUserAsync(UserName userName, string password) {
		var checkTask = Task.Run(() => BindConnection(userName, password));
		var timeoutTask = Task.Run(async () => {
			await Task.Delay(_options.ConnectionTimeout);
			return false;
		});
		var task = await Task.WhenAny(checkTask, timeoutTask);
		if (task == timeoutTask) {
			_logger.LogWarning("LDAP user find: timeout ({ConnectionTimeout})", _options.ConnectionTimeout);
		}
		return await task;
	}

	private bool BindConnection(UserName userName, string password) {
		using LdapConnection connection = new(_options.Host);
		NetworkCredential credential = new(userName.Name, password, userName.Domain);
		try {
			connection.Bind(credential);
			return true;
		}
		catch (Exception e) {
			_logger.LogWarning(e, "Error during user [{User}] auth", userName);
		}
		return false;
	}
}