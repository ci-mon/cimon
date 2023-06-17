using Cimon.Contracts;
using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monitor = Cimon.DB.Models.Monitor;
using User = Cimon.DB.Models.User;

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
		await InitDemoMonitors(context);
	}

	private static async Task InitDemoMonitors(CimonDbContext context) {
		var buildConfig = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "BpmsPlatformWorkDiagnostic",
			CISystem = CISystem.TeamCity,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=BpmsPlatformWorkDiagnostic&tab=buildTypeStatusDiv",
				ProjectName = "Team Diagnostics",
				Name = "BpmsPlatformWorkDiagnostic",
				Number = "8.1.0.0",
				StatusText = "Tests passed: 339, ignored: 10, muted: 2",
				Status = 0,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "",
				BuildConfigId = "BpmsPlatformWorkDiagnostic"
			}
		});
		await context.Monitors.AddAsync(new Monitor() {
			Key = "Test 1",
			Title = "Test mon 1",
			Builds = {
				buildConfig.Entity
			}
		});
	}
}
