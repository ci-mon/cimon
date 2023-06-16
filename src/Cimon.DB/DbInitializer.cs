using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cimon.DB;

public class DbSeedOptions
{
	public bool UseTestData { get; set; }
}

public class DbInitializer
{
	private readonly CimonDbContext _dbContext;
	private readonly DbSeedOptions _options;

	public DbInitializer(CimonDbContext dbContext, IOptions<DbSeedOptions> options) {
		_dbContext = dbContext;
		_options = options.Value;
	}

	public static async Task Init(IServiceProvider serviceProvider) {
		using var serviceScope = serviceProvider.CreateScope();
		var services = serviceScope.ServiceProvider;
		var initializer = services.GetRequiredService<DbInitializer>();
		await initializer.Init();
	}

	public async Task Init() {
		await _dbContext.Database.EnsureCreatedAsync();
		if (_options.UseTestData) {
			await AddTestData(_dbContext);
		}
		await _dbContext.SaveChangesAsync();
	}

	private static async Task AddTestData(CimonDbContext context) {
		if (await context.Users.AnyAsync()) {
			return;
		}
		var adminTeam = await context.Teams.AddAsync(new Team {
			Name = "admins"
		});
		var usersTeam = await context.Teams.AddAsync(new Team {
			Name = "users"
		});
		var allTeam = await context.Teams.AddAsync(new Team {
			Name = "all"
		});
		var monitorEditorRole = await context.Roles.AddAsync(new Role {
			Name = "monitor-editor"
		});
		var teamsEditorRole = await context.Roles.AddAsync(new Role {
			Name = "teams-editor"
		});
		var allEditorRole = await context.Roles.AddAsync(new Role {
			Name = "all-editor",
			OwnedRoles = {
				monitorEditorRole.Entity,
				teamsEditorRole.Entity
			}
		});
		var adminRole = await context.Roles.AddAsync(new Role {
			Name = "admin",
			OwnedRoles = {
				allEditorRole.Entity
			}
		});
		await context.Users.AddAsync(new User
			{ Name = "test", FullName = "Test User", AllowLocalLogin = true,
				Teams = { usersTeam.Entity, allTeam.Entity } });
		await context.Users.AddAsync(new User {
			Name = "admin", FullName = "Test Admin", Roles = { adminRole.Entity }, AllowLocalLogin = true,
			Teams = { adminTeam.Entity, allTeam.Entity }
		});
	}
}
