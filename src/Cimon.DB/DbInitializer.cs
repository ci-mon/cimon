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
		var buildConfig1 = await context.BuildConfigurations.AddAsync(new BuildConfig {
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
		var buildConfig2 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "Unit",
			CISystem = CISystem.TeamCity,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				ProjectName = "Core",
				Name = "Unit",
				Number = "8.1.0.0 ",
				StatusText = "Tests passed: 23760, ignored: 31, muted: 3",
				Status = 0,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "",
				BuildConfigId = "Unit"
			}
		});
		var buildConfig3 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "Unit (.Net 6)",
			CISystem = CISystem.TeamCity,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				ProjectName = "Core",
				Name = "Unit (.Net 6)",
				Number = "8.1.0.0 ",
				StatusText = "Tests passed: 21343, ignored: 384, muted: 4",
				Status = 0,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "",
				BuildConfigId = "Unit (.Net 6)",
			}
		});
		var buildConfig4 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "Integration (MSSQL)",
			CISystem = CISystem.TeamCity,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				ProjectName = "Core",
				Name = "Integration (MSSQL)",
				Number = "8.1.0.610 ProductBase Softkey ENU",
				StatusText = "Tests passed: 1723, ignored: 30, muted: 4",
				Status = 0,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "",
				BuildConfigId = "Integration (MSSQL)",
			}
		});
		var buildConfig5 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "Integration (PostgreSQL)",
			CISystem = CISystem.TeamCity,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				ProjectName = "Core",
				Name = "Integration (PostgreSQL)",
				Number = "8.1.0.610 ProductBase Softkey ENU",
				StatusText = "Tests passed: 1723, ignored: 30, muted: 4",
				Status = 0,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "",
				BuildConfigId = "Integration (PostgreSQL)",
			}
		});
		var buildConfig6 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "Integration (Oracle)",
			CISystem = CISystem.TeamCity,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				ProjectName = "Core",
				Name ="Integration (Oracle)",
				Number = "8.1.0.601 ProductBase Softkey ENU",
				StatusText = "Tests passed: 1710, ignored: 55, muted: 7",
				Status = 0,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "",
				BuildConfigId = "Integration (Oracle)",
			}
		});
		var buildConfig7 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "app.studio-enterprise.shell",
			CISystem = CISystem.Jenkins,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.shell/job/master/4850/",
				ProjectName = "Core",
				Name = "app.studio-enterprise.shell",
				Number = "",
				Status = BuildStatus.Failed,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Committers = "test,admin",
				BuildConfigId = "app.studio-enterprise.shell",
			}
		});
		var buildConfig8 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "app.studio-enterprise.schema-view",
			CISystem = CISystem.Jenkins,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/",
				ProjectName = "Core",
				Name = "app.studio-enterprise.schema-view",
				Number = "",
				Status = BuildStatus.Failed,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				Committers = "test,admin",
				BuildConfigId = "app.studio-enterprise.schema-view",
			}
		});
		var buildConfig9 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "app.studio-enterprise.process-designer",
			CISystem = CISystem.Jenkins,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/",
				ProjectName = "Core",
				Name = "app.studio-enterprise.process-designer",
				Number = "",
				Status = BuildStatus.Success,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				Committers = "test,admin",
				BuildConfigId = "app.studio-enterprise.process-designer",
			}
		});
		var buildConfig10 = await context.BuildConfigurations.AddAsync(new BuildConfig {
			Key = "lib.studio-enterprise.process",
			CISystem = CISystem.Jenkins,
			DemoState = new BuildInfo {
				BuildHomeUrl = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/",
				ProjectName = "Core",
				Name = "lib.studio-enterprise.process",
				Number = "",
				Status = BuildStatus.Success,
				FinishDate = DateTime.Now,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				Committers = "test,admin",
				BuildConfigId = "lib.studio-enterprise.process",
			}
		});
		await context.Monitors.AddAsync(new Monitor() {
			Key = "Test 1",
			Title = "Test mon 1",
			Builds = {
				buildConfig1.Entity, 
				buildConfig2.Entity, 
				buildConfig3.Entity, 
				buildConfig4.Entity, 
				buildConfig5.Entity, 
				buildConfig6.Entity, 
			}
		});
		await context.Monitors.AddAsync(new Monitor() {
			Key = "All",
			Title = "All",
			Builds = {
				buildConfig1.Entity, 
				buildConfig2.Entity, 
				buildConfig3.Entity, 
				buildConfig4.Entity, 
				buildConfig5.Entity, 
				buildConfig6.Entity, 
				buildConfig7.Entity, 
				buildConfig8.Entity, 
				buildConfig9.Entity, 
				buildConfig10.Entity, 
			}
		});
	}
}
