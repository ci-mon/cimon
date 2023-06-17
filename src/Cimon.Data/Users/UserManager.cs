using System.DirectoryServices.Protocols;
using System.Net;
using Cimon.Contracts;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using User = Cimon.Contracts.User;

namespace Cimon.Data.Users;

public class UserManager
{
	private readonly ILogger _logger;
	private readonly CimonDbContext _dbContext;
	private readonly CimonDataSettings _cimonDataSettings;

	public UserManager(ILogger<UserManager> logger, CimonDbContext dbContext, 
			IOptions<CimonDataSettings> cimonDataSettings) {
		_logger = logger;
		_dbContext = dbContext;
		_cimonDataSettings = cimonDataSettings.Value;
	}

	public async Task Deactivate(UserName name) {
		var user = await _dbContext.Users.SingleAsync(x => x.Name == name.Name);
		user.IsDeactivated = true;
		await _dbContext.SaveChangesAsync();
	}
	
	public async Task Activate(UserName name) {
		var user = await _dbContext.Users.SingleAsync(x => x.Name == name.Name);
		user.IsDeactivated = false;
		await _dbContext.SaveChangesAsync();
	}

	public async Task<bool> IsDeactivated(UserName name) {
		var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Name == name.Name);
		return user?.IsDeactivated ?? false;
	}

	private IReadOnlyCollection<string> GetRoles(List<Role> rootRoles, List<Role> allRoles) {
		var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var toProcess = new Queue<Role>(rootRoles);
		while (toProcess.Count > 0) {
			var role = toProcess.Dequeue();
			if (!result.Add(role.Name)) continue;
			foreach (var owned in role.OwnedRoles) {
				var ownedWithData = allRoles.First(x => x.Id == owned.Id);
				toProcess.Enqueue(ownedWithData);
			}
		}
		return result;
	}

	public async Task<User?> FindOrCreateUser(UserName name) {
		var user = await _dbContext.Users
			.Include(x => x.Roles)
			.Include(x => x.Teams)
			.FirstOrDefaultAsync(x => x.Name == name.Name);
		if (user != null) {
			var teams = user.Teams.Select(x => x.Name).ToList();
			var allRoles = await _dbContext.Roles.Include(x => x.OwnedRoles).ToListAsync();
			var roles = GetRoles(user.Roles, allRoles);
			return new User(user.Name, user.Name, teams, roles);
		}
		var allTeams = await _dbContext.Teams.ToListAsync();
		/*
		 TODO create user
		 var server = $"{domain}.com";
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
		var name = userName.Name;
		if (_cimonDataSettings.IsDevelopment &&
				await _dbContext.Users.AnyAsync(u=>u.Name == name && u.AllowLocalLogin)) {
			return true;
		}
		string domain = userName.Domain.ToLowerInvariant(); // TODO get from where?
		var server = $"{domain}.com";
		LdapConnection connection = new(server);
		NetworkCredential credential = new(name, password, userName.Domain);
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
