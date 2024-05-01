using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monitor = Cimon.DB.Models.MonitorModel;
using User = Cimon.DB.Models.User;

namespace Cimon.DB;

using System.Collections.Immutable;
using Contracts.CI;
using Microsoft.EntityFrameworkCore.ChangeTracking;

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
		await _dbContext.Database.MigrateAsync();
		await AddTestData(_dbContext);
		await _dbContext.SaveChangesAsync();
	}

	private async Task AddTestData(CimonDbContext context) {
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
			Name = "all",
			ChildTeams = {
				adminTeam.Entity
			}
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
		await context.AddAsync(new CIConnector {
			Key = "teamcity_main",
			CISystem = CISystem.TeamCity
		});
		await context.AddAsync(new CIConnector {
			Key = "jenkins_main",
			CISystem = CISystem.Jenkins
		});
		if (!_options.UseTestData) {
			return;
		}
		var demoConnector = await context.AddAsync(new CIConnector {
			Key = "demo_main",
			CISystem = CISystem.Demo
		});
		await context.Users.AddAsync(new User
			{ Name = "test", FullName = "Test User", Email = "milton.soto@example.com", AllowLocalLogin = true,
				Teams = { usersTeam.Entity, allTeam.Entity } });
		await context.Users.AddAsync(new User {
			Name = "admin", FullName = "Test Admin", Email = "bedete.araujo@example.com", Roles = { adminRole.Entity }, AllowLocalLogin = true,
			Teams = { adminTeam.Entity }
		});
		await InitDemoMonitors(context, demoConnector);
	}

	private static async Task InitDemoMonitors(CimonDbContext context, EntityEntry<CIConnector> demoConnector) {
		var buildConfig1 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "Cake_CakeMaster") {
			DemoState = new BuildInfo {
				Url = "https://teamcity.jetbrains.com/buildConfiguration/Cake_CakeMaster/4486115",
				Group = "Cake",
				Name = "Cake Develop",
				Id = "42",
				StatusText = "Tests passed: 339, ignored: 10, muted: 2",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
			},
			IsDefaultBranch = true
		});
		var buildConfig2 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "TeamCityPluginsByJetBrains_NUnitTeamCity_NUnitIntegration") {
			DemoState = new BuildInfo {
				Url = "https://teamcity.jetbrains.com/buildConfiguration/TeamCityPluginsByJetBrains_NUnitTeamCity_NUnitIntegration/4342886",
				Group = "Unit",
				Name = "NUnit Integration",
				Id = "42 ",
				StatusText = "Tests passed: 23760, ignored: 31, muted: 3",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
			},
			IsDefaultBranch = true
		});
		var buildConfig3 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "Unit (.Net 6)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity.jetbrains.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Unit (.Net 6)",
				Id = "42 ",
				StatusText = "Tests passed: 21343, ignored: 384, muted: 4",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
			},
			IsDefaultBranch = true
		});
		var buildConfig4 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "Integration (MSSQL)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity.jetbrains.com/viewType.html?buildTypeId=int_tests&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Integration (MSSQL)",
				Id = "42",
				StatusText = "Tests passed: 1723, ignored: 30, muted: 4",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
			},
			IsDefaultBranch = true
		});
		var manyChanges = Enumerable.Range(0, 10).Select(i => 
				new VcsChange(new VcsUser($"test{i}", $"Test {i}", $"user{i}@example.com"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty))
			.Prepend(new VcsChange(new VcsUser("admin", "Admin", "bedete.araujo@example.com"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty))
			.ToArray();
		var buildConfig5 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "Integration (PostgreSQL)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity.jetbrains.com/viewType.html?buildTypeId=tests&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Integration (PostgreSQL)",
				Id = "42",
				StatusText = "Tests passed: 1723, ignored: 30, muted: 4",
				Status = BuildStatus.Failed,
				StartDate = DateTime.Now.AddHours(-1),
				Duration = TimeSpan.FromSeconds(1234),
				BranchName = "trunk",
				Changes = manyChanges,
				FailureSuspects = ImmutableList.Create(new BuildFailureSuspect(manyChanges[0].Author, 82))
			},
			IsDefaultBranch = true
		});
		var buildConfig6 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "Integration (Oracle)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity.jetbrains.com/viewType.html?buildTypeId=oracle_tests&tab=buildTypeStatusDiv",
				Group = "Core",
				Name ="Integration (Oracle)",
				Id = "42",
				StatusText = "Tests passed: 1710, ignored: 55, muted: 7",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
			},
			IsDefaultBranch = true
		});
		var buildConfig7 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "app.scope1.app1") {
			DemoState = new BuildInfo {
				Url = "https://localhost:8080/job/app.scope1.app1/job/master/4850/",
				Group = "Core",
				Name = "app.scope1.app1",
				Id = "1",
				Status = BuildStatus.Failed,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Changes = manyChanges,
			},
			IsDefaultBranch = true
		});
		var buildConfig8 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "jen_proj2") {
			DemoState = new BuildInfo {
				Url = "https://localhost:8080/job/jen_proj2/job/master/9248/",
				Group = "Core",
				Name = "jen_proj2",
				Id = "2",
				Status = BuildStatus.Failed,
				BranchName = "",
				Changes = new[] {
					new VcsChange(new VcsUser("test", "Test"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty),
					new VcsChange(new VcsUser("admin", "Admin"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty)
				},
			},
			IsDefaultBranch = true
		});
		var buildConfig9 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "app.scope2.app3") {
			DemoState = new BuildInfo {
				Url = "https://localhost:8080/job/app.scope2.schema-view/job/master/9248/",
				Group = "Core",
				Name = "app.scope2.app3",
				Id = "3",
				Status = BuildStatus.Success,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				Changes = new[] {
					new VcsChange(new VcsUser("test", "Test"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty),
					new VcsChange(new VcsUser("admin", "Admin"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty)
				},
			},
			IsDefaultBranch = true
		});
		var buildConfig10 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(demoConnector.Entity, "lib.scope2.app4") {
			DemoState = new BuildInfo {
				Url = "https://localhost:8080/job/app.scope2.schema-view/job/master/9248/",
				Group = "Core",
				Name = "lib.scope2.app4",
				Id = "4",
				Status = BuildStatus.Success,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				StatusText = "something works, something not (not stable)",
				Changes = new[] {
					new VcsChange(new VcsUser("test", "Test"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty),
					new VcsChange(new VcsUser("admin", "Admin"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty)
				},
			},
			IsDefaultBranch = true
		});

		async Task AddBuildsToMonitor(Monitor monitor, params BuildConfigModel[] buildConfigs) {
			foreach (var config in buildConfigs) {
				await context.MonitorBuilds.AddAsync(new BuildInMonitor {
					Monitor = monitor,
					MonitorId = monitor.Id,
					BuildConfig = config,
					BuildConfigId = config.Id
				});
			}
		}
		var mon1 = await context.Monitors.AddAsync(new Monitor() {
			Key = "Test 1",
			Title = "Test mon 1",
			Shared = true
		});
		await AddBuildsToMonitor(mon1.Entity,
			buildConfig1.Entity,
			buildConfig2.Entity,
			buildConfig3.Entity,
			buildConfig4.Entity,
			buildConfig5.Entity,
			buildConfig6.Entity);
		var mon2 = await context.Monitors.AddAsync(new Monitor() {
			Key = "All",
			Title = "All",
			Shared = true
		});
		await AddBuildsToMonitor(mon2.Entity,
			buildConfig1.Entity,
			buildConfig2.Entity,
			buildConfig3.Entity,
			buildConfig4.Entity,
			buildConfig5.Entity,
			buildConfig6.Entity,
			buildConfig7.Entity,
			buildConfig8.Entity,
			buildConfig9.Entity,
			buildConfig10.Entity);
		var monGroup = await context.Monitors.AddAsync(new Monitor() {
			Key = "Group",
			Title = "Group",
			Shared = true,
			Type = MonitorType.Group
		});
		monGroup.Entity.ConnectedMonitors = [
			new ConnectedMonitor {
				SourceMonitorModel = monGroup.Entity,
				ConnectedMonitorModel = mon1.Entity
			},
			new ConnectedMonitor {
				SourceMonitorModel = monGroup.Entity,
				ConnectedMonitorModel = mon2.Entity
			}
		];
	}
	
}
