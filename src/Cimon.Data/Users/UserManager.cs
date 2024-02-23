using System.Collections.Concurrent;
using System.Collections.Immutable;
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

using System.Runtime.InteropServices;
using System.Security.Principal;

public class UserManager : ITechnicalUsers
{
	private record UserCache(UserModel Model, User User);

	private const string TeamClaimName = "team";
	
	private readonly ILogger _logger;
	private readonly IDbContextFactory<CimonDbContext> _dbContextFactory;
	private readonly CimonDataSettings _cimonDataSettings;
	private readonly LdapClient _ldapClient;
	private readonly ConcurrentDictionary<UserName, Task<UserCache?>> _cachedUsers = new();

	public UserManager(ILogger<UserManager> logger, IDbContextFactory<CimonDbContext> dbContextFactory, 
			IOptions<CimonDataSettings> cimonDataSettings, LdapClient ldapClient) {
		_logger = logger;
		_dbContextFactory = dbContextFactory;
		_ldapClient = ldapClient;
		_cimonDataSettings = cimonDataSettings.Value;
	}

	private async Task EditUser(UserName name, Action<UserModel> action) => await EditUser(name, user => {
		action(user);
		return true;
	});

	private async Task EditUser(UserName name, Func<UserModel, bool> action) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
		var user = await dbContext.Users.SingleAsync(x => x.Name == name.Name);
		if (action(user)) {
			await dbContext.SaveChangesAsync();
			_cachedUsers.Remove(name, out _);
		}
	}

	public async Task Deactivate(UserName name) => await EditUser(name, user => {
		user.IsDeactivated = true;
	});

	public async Task Activate(UserName name) => await EditUser(name, user => {
		user.IsDeactivated = false;
	});

	public async Task<bool> IsDeactivated(UserName name) {
		var userCache = await GetUserCache(name);
		// if we have no user in db we should not try to sign out him from cookie scheme
		return userCache?.Model.IsDeactivated ?? false;
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
				Claims = claims,
				DefaultMonitorId = userModel.DefaultMonitorId,
				Id = userModel.Id
			};
			return new UserCache(userModel, user);
		}
		return null;
	}

	public async Task<User?> FindOrCreateUser(UserName name) {
		UserCache? userCache = await GetUserCache(name);
		if (userCache is not null) {
			return userCache.User;
		}
		await CreateUserInDb(name);
		_cachedUsers.Remove(name.Name, out _);
		userCache = await GetUserCache(name);
		return userCache?.User;
	}

	private async Task CreateUserInDb(UserName name) {
		if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
			return;
		}
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
		var userInfo = _ldapClient.FindUserInfo(name.Name);
		var user = new Cimon.DB.Models.User {
			Name = userInfo.SamAccountName,
			FullName = userInfo.DisplayName,
			Email = userInfo.EmailAddress
		};
		foreach (var teamName in userInfo.Teams) {
			Team? team = await dbContext.Teams.FirstOrDefaultAsync(t => t.Name == teamName);
			if (team is null) {
				team = new Team {
					Name = teamName
				};
				dbContext.Teams.Add(team);
				await dbContext.SaveChangesAsync();
			}
			user.Teams.Add(team);
		}
		if (userInfo.IsAdmin) {
			if (await dbContext.Teams.FirstOrDefaultAsync(x => x.Name == "admins") is { } adminsTeam) {
				user.Teams.Add(adminsTeam);
			}
			if (await dbContext.Roles.FirstOrDefaultAsync(x => x.Name == "admin") is { } adminRole) {
				user.Roles.Add(adminRole);
			}
		}
		dbContext.Users.Add(user);
		await dbContext.SaveChangesAsync();
	}

	public async Task<bool> SignInAsync(UserName userName, string password) {
		var name = userName.Name;
		if (_cimonDataSettings.IsDevelopment) {
			await using var dbContext = await _dbContextFactory.CreateDbContextAsync(); 
			if (await dbContext.Users.AnyAsync(u=>u.Name == name && u.AllowLocalLogin)) {
				return true;
			}
		}
		return await _ldapClient.FindUserAsync(name, password);
	}

	public async IAsyncEnumerable<UserInfo> GetUsers(string? searchTerm) {
		await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
		var users = dbContext.Users.Include(x => x.Teams).ThenInclude(x => x.ChildTeams)
			.Where(u => EF.Functions.Like(u.FullName, $"%{searchTerm}%"))
			.ToAsyncEnumerable();
		await foreach (var user in users) {
			// TODO how to find main team? 
			var mainTeam = user.Teams.FirstOrDefault(t => !t.ChildTeams.Any());
			yield return new UserInfo(user.Name, user.FullName, mainTeam?.Name, user.Teams.Select(CreateTeamInfo).ToImmutableList());
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

	private string? FindClaim(ClaimsPrincipal? principal, string claimName) => 
		principal?.Claims.FirstOrDefault(x => x.Type == claimName)?.Value;

	public async Task<User> GetUser(ClaimsPrincipal? principal) {
		IIdentity? identity = principal?.Identity;
		string? name = FindClaim(principal, ClaimTypes.NameIdentifier) ?? FindClaim(principal, ClaimTypes.Name);
		if (identity is null || !identity.IsAuthenticated || name is null) {
			return User.Guest;
		}
		return await GetUser(name);
	}

	public async Task<User> GetUser(string name) {
		UserCache? userCache = await GetUserCache(name);
		return userCache?.User ?? User.Guest;
	}

	private Task<UserCache?> GetUserCache(UserName name) => _cachedUsers.GetOrAdd(name.Name, FindUser);

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
	
	public User MonitoringBot { get; } = User.Create("monitoring.bot", "Monitoring bot", -2);

	public async Task SaveLastViewedMonitorId(UserName name, string? monitorId) {
		await EditUser(name, user => {
			if (user.DefaultMonitorId?.Equals(monitorId, StringComparison.OrdinalIgnoreCase) == true) {
				return false;
			}
			user.DefaultMonitorId = monitorId;
			return true;
		});
	}
}
