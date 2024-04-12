using System.DirectoryServices.Protocols;
using System.Net;
using System.Runtime.InteropServices;
using System.Security;
using Cimon.Contracts;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cimon.Data.Users;

using System.DirectoryServices.AccountManagement;
using System.Runtime.Versioning;

public class LdapClientSecrets
{
	public string Host { get; set; } = "dc.com";
	public TimeSpan ConnectionTimeout { get; set; }
	public string[] TeamGroups { get; set; } = Array.Empty<string>();
	public string[] AdminGroups { get; set; } = Array.Empty<string>();
}

public class LdapUserInfo
{
	public required string SamAccountName { get; init; }
	public required string DisplayName { get; init; }
	public required string EmailAddress { get; init; }
	public List<string> Teams { get; init; } = new();
	public bool IsAdmin { get; set; }
}

public class LdapClient : IHealthCheck
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

	[SupportedOSPlatform("windows")]
	public LdapUserInfo GetUserInfo(string name) {
		if (string.IsNullOrWhiteSpace(_options.Host)) {
			throw new NotSupportedException("Ldap is not configured");
		}
		using var context = new PrincipalContext(ContextType.Domain, _options.Host);
		UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, name) ??
			throw new SecurityException($"User {name} is not found in {_options.Host}");
		var user = new LdapUserInfo {
			SamAccountName = userPrincipal.SamAccountName,
			DisplayName = userPrincipal.DisplayName,
			EmailAddress = userPrincipal.EmailAddress?.ToLowerInvariant() ?? string.Empty
		};
		var teamGroups = _options.TeamGroups.ToHashSet();
		foreach (Principal group in userPrincipal.GetGroups(context)) {
			bool isValidTeam = teamGroups.Contains(group.Name) || teamGroups.Any(g => IsMember(group, g, context));
			if (isValidTeam) {
				user.Teams.Add(group.SamAccountName);
				if (_options.AdminGroups.Any(g => g.Equals(group.Name, StringComparison.OrdinalIgnoreCase))) {
					user.IsAdmin = true;
				}
			}
		}
		return user;
	}

	[SupportedOSPlatform("windows")]
	private bool IsMember(Principal group, string groupToCheck, PrincipalContext context) {
		try {
			return group.IsMemberOf(context, IdentityType.SamAccountName, groupToCheck);
		} catch (Exception e) {
			_logger.LogError(e, "Failed to validate team: {GroupToCheck} {Group}", groupToCheck, 
				group.DisplayName);
			return false;
		}
	}

	public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken()) {
		try {
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
				return Task.FromResult(HealthCheckResult.Unhealthy("OS not supported"));
			}
			if (string.IsNullOrWhiteSpace(_options.Host)) {
				return Task.FromResult(HealthCheckResult.Healthy("disabled"));
			}
			using var ctx = new PrincipalContext(ContextType.Domain, _options.Host);
			return Task.FromResult(HealthCheckResult.Healthy(_options.Host));
		} catch (Exception e) {
			return Task.FromResult(HealthCheckResult.Unhealthy(e.Message));
		}
	}
}
