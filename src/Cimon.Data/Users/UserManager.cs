using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Security.Claims;
using Cimon.Contracts;
using Cimon.DB;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using User = Cimon.Contracts.User;
using UserModel = Cimon.DB.Models.User;

namespace Cimon.Data.Users;

public class UserManager : ITechnicalUsers
{
	private record UserCache(UserModel Model, User User);
	
	private const string TeamClaimName = "team";
	
	private readonly ILogger _logger;
	private readonly IDbContextFactory<CimonDbContext> _dbContextFactory;
	private readonly CimonDataSettings _cimonDataSettings;
	private readonly ConcurrentDictionary<UserName, Task<UserCache?>> _cachedUsers = new();

	public UserManager(ILogger<UserManager> logger, IDbContextFactory<CimonDbContext> dbContextFactory, 
			IOptions<CimonDataSettings> cimonDataSettings) {
		_logger = logger;
		_dbContextFactory = dbContextFactory;
		_cimonDataSettings = cimonDataSettings.Value;
	}

	public async Task Deactivate(UserName name) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
		var user = await dbContext.Users.SingleAsync(x => x.Name == name.Name);
		user.IsDeactivated = true;
		await dbContext.SaveChangesAsync();
		_cachedUsers.Remove(name, out _);
	}
	
	public async Task Activate(UserName name) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
		var user = await dbContext.Users.SingleAsync(x => x.Name == name.Name);
		user.IsDeactivated = false;
		await dbContext.SaveChangesAsync();
		_cachedUsers.Remove(name, out _);
	}

	public async Task<bool> IsDeactivated(UserName name) {
		var userCache = await GetUserCache(name);
		return userCache?.Model.IsDeactivated ?? true;
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

	private async Task<UserCache?> FindUser(UserName name) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
		var userModel = await dbContext.Users
			.Include(x => x.Roles)
			.Include(x => x.Teams)
			.FirstOrDefaultAsync(x => x.Name == name.Name);
		if (userModel != null) {
			var teams = userModel.Teams.Select(x => x.Name).ToList();
			var allRoles = await dbContext.Roles.Include(x => x.OwnedRoles).ToListAsync();
			var roles = GetRoles(userModel.Roles, allRoles);
			var claims = CreateClaims(userModel, roles, teams);
			var user = new User(userModel.Name, userModel.FullName, teams, roles) {
				Claims = claims
			};
			return new UserCache(userModel, user);
		}
		return null;
	}

	public async Task<User?> FindOrCreateUser(UserName name) {
		var user = await FindUser(name);
		if (user is not null) {
			return user.User;
		}
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
		var allTeams = await dbContext.Teams.ToListAsync();
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
		if (_cimonDataSettings.IsDevelopment) {
			await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
			if (await dbContext.Users.AnyAsync(u=>u.Name == name && u.AllowLocalLogin)) {
				return true;
			}
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

	public async IAsyncEnumerable<UserInfo> GetUsers(string? searchTerm) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
		var users = dbContext.Users.Include(x => x.Teams).ThenInclude(x => x.ChildTeams)
			.Where(u => EF.Functions.Like(u.FullName, $"%{searchTerm}%"))
			.ToAsyncEnumerable();
		await foreach (var user in users) {
			yield return new UserInfo(user.Name, user.FullName, user.Teams.Select(CreateTeamInfo).ToImmutableList());
		}
	}

	public async IAsyncEnumerable<TeamInfo> GetTeams(string? searchTerm, bool childOnly = true) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
		var teams = dbContext.Teams.Include(x => x.ChildTeams)
			.Where(t => EF.Functions.Like(t.Name, $"%{searchTerm}%"))
			.Where(x => !childOnly || x.ChildTeams.Count == 0)
			.ToAsyncEnumerable();
		await foreach (var team in teams) {
			yield return CreateTeamInfo(team);
		}
	}

	private static TeamInfo CreateTeamInfo(Team x) {
		return new TeamInfo(x.Name, x.ChildTeams.Select(c=>c.Name).ToImmutableList());
	}

	public async Task<User> GetUser(ClaimsPrincipal? principal) {
		var identity = principal?.Identity;
		var name = principal?.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
		if (identity is null || !identity.IsAuthenticated || name is null) {
			return User.Guest;
		}
		var userCache = await GetUserCache((UserName)name);
		return userCache?.User ?? User.Guest;
	}

	private Task<UserCache?> GetUserCache(UserName name) => _cachedUsers.GetOrAdd(name, FindUser);

	private IImmutableList<Claim> CreateClaims(UserModel user, IEnumerable<string> roles,
			IEnumerable<string> teams) {
		var claims = new List<Claim> {
			new(ClaimTypes.NameIdentifier, user.Name),
			new(ClaimTypes.Name, user.FullName)
		};
		if (user.Email is not null) {
			claims.Add(new(ClaimTypes.Email, user.Email));
		}
		claims.AddRange(teams.Select(x => new Claim(TeamClaimName, x)));
		claims.AddRange(roles.Select(x => new Claim(ClaimTypes.Role, x)));
		return claims.ToImmutableList();
	}
	
	public User MonitoringBot { get; } = User.Create("monitoring.bot", "Monitoring bot");
}
