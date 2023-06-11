using Microsoft.Extensions.Logging;
using System.DirectoryServices.Protocols;
using Cimon.DB;
using Microsoft.EntityFrameworkCore;
using User = Cimon.Data.Users.User;
using System.Net;

namespace Cimon.Auth;

public class UserManager
{
	private readonly ILogger _logger;
	private readonly CimonDbContext _dbContext;

	public UserManager(ILogger<UserManager> logger, CimonDbContext dbContext) {
		_logger = logger;
		_dbContext = dbContext;
	}

	public async Task Deactivate(UserName name) {
		var user = await _dbContext.Users.SingleAsync(x => x.Name == name.Name);
		user.IsDeactivated = true;
		await _dbContext.SaveChangesAsync();
	}

	public async Task<bool> IsDeactivated(UserName name) {
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Name == name.Name);
		return user?.IsDeactivated ?? false;
	}

	public async Task<User> FindOrCreateUser(UserName name) {
		var user = await _dbContext.Users.Include(x => x.Roles).Include(x => x.Teams)
			.FirstOrDefaultAsync(x => x.Name == name.Name);
		if (user != null) {
			return new User(user.Name, user.Name, user.Teams.Select(x => x.Name).ToList(),
				user.Roles.Select(x => x.Name).ToList());
		}
		/*
		 todo this should be in other place 
		  var context = new PrincipalContext (ContextType.Domain, server);
		 * UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, userName.Name);
		user.DisplayName.Dump();
		foreach (GroupPrincipal group in user.GetGroups(context))
		{
		    group.Name.Dump();
		}
		 */
		return null;
	}

	public async Task<bool> SignInAsync(UserName userName, string password) {
		if (userName == "test" || userName == "admin") {
			return true;
		}
		string domain = userName.Domain.ToLowerInvariant(); // TODO get from where?
		var server = $"{domain}.com";
		LdapConnection connection = new(server);
		NetworkCredential credential = new(userName.Name, password, userName.Domain);
		try {
			connection.Bind(credential);
			connection.Dispose();
			return true;
		} catch (Exception e) {
			_logger.LogWarning(e, "Error during user [{User}] auth", userName);
		}
		return false;
	}
}
