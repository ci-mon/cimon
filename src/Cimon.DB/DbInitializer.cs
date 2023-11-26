using Cimon.DB.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Monitor = Cimon.DB.Models.MonitorModel;
using User = Cimon.DB.Models.User;

namespace Cimon.DB;

using System.Collections.Immutable;
using Contracts.CI;

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
		// https://vinicius73.github.io/gravatar-url-generator/#/
		await context.Users.AddAsync(new User
			{ Name = "test", FullName = "Test User", Email = "milton.soto@example.com", AllowLocalLogin = true,
				Teams = { usersTeam.Entity, allTeam.Entity } });
		await context.Users.AddAsync(new User {
			Name = "admin", FullName = "Test Admin", Email = "bedete.araujo@example.com", Roles = { adminRole.Entity }, AllowLocalLogin = true,
			Teams = { adminTeam.Entity }
		});
		await InitDemoMonitors(context);
	}

	private static async Task InitDemoMonitors(CimonDbContext context) {
		var teamcityConnector = await context.AddAsync(new CIConnector {
			Key = "teamcity_main",
			CISystem = CISystem.TeamCity
		});
		var jenkinsConnector = await context.AddAsync(new CIConnector {
			Key = "jenkins_main",
			CISystem = CISystem.Jenkins
		});
		var buildConfig1 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(teamcityConnector.Entity, "BpmsPlatformWorkDiagnostic") {
			DemoState = new BuildInfo {
				Url = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=BpmsPlatformWorkDiagnostic&tab=buildTypeStatusDiv",
				Group = "Team Diagnostics",
				Name = "BpmsPlatformWorkDiagnostic",
				Number = "8.1.0.0",
				StatusText = "Tests passed: 339, ignored: 10, muted: 2",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				BuildConfigId = 1
			}
		});
		var buildConfig2 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(teamcityConnector.Entity, "Unit") {
			DemoState = new BuildInfo {
				Url = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Unit",
				Number = "8.1.0.0 ",
				StatusText = "Tests passed: 23760, ignored: 31, muted: 3",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				BuildConfigId = 2
			}
		});
		var buildConfig3 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(teamcityConnector.Entity, "Unit (.Net 6)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Unit (.Net 6)",
				Number = "8.1.0.0 ",
				StatusText = "Tests passed: 21343, ignored: 384, muted: 4",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				BuildConfigId = 3
			}
		});
		var buildConfig4 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(teamcityConnector.Entity, "Integration (MSSQL)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Integration (MSSQL)",
				Number = "8.1.0.610 ProductBase Softkey ENU",
				StatusText = "Tests passed: 1723, ignored: 30, muted: 4",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				BuildConfigId = 4
			}
		});
		var manyChanges = Enumerable.Range(0, 10).Select(i => 
				new VcsChange(new VcsUser($"test{i}", $"Test {i}", $"user{i}@example.com"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty))
			.Prepend(new VcsChange(new VcsUser("admin", "Admin", "bedete.araujo@example.com"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty))
			.ToArray();
		var buildConfig5 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(teamcityConnector.Entity, "Integration (PostgreSQL)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				Group = "Core",
				Name = "Integration (PostgreSQL)",
				Number = "8.1.0.610 ProductBase Softkey ENU",
				StatusText = "Tests passed: 1723, ignored: 30, muted: 4",
				Status = BuildStatus.Failed,
				StartDate = DateTime.Now.AddHours(-1),
				Duration = TimeSpan.FromSeconds(1234),
				BranchName = "trunk",
				BuildConfigId = 5,
				Changes = manyChanges,
				FailureSuspect = new BuildFailureSuspect(manyChanges[0].Author, 82)
			}
		});
		var buildConfig6 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(teamcityConnector.Entity, "Integration (Oracle)") {
			DemoState = new BuildInfo {
				Url = "https://teamcity-rnd.bpmonline.com/viewType.html?buildTypeId=ContinuousIntegration_UnitTest_780_PreCommitUnitTest&tab=buildTypeStatusDiv",
				Group = "Core",
				Name ="Integration (Oracle)",
				Number = "8.1.0.601 ProductBase Softkey ENU",
				StatusText = "Tests passed: 1710, ignored: 55, muted: 7",
				Status = 0,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				BuildConfigId = 6,
			}
		});
		var buildConfig7 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(jenkinsConnector.Entity, "app.studio-enterprise.shell") {
			DemoState = new BuildInfo {
				Url = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.shell/job/master/4850/",
				Group = "Core",
				Name = "app.studio-enterprise.shell",
				Number = "",
				Status = BuildStatus.Failed,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "trunk",
				Changes = manyChanges,
				BuildConfigId = 7,
			}
		});
		var buildConfig8 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(jenkinsConnector.Entity, "app.studio-enterprise.schema-view") {
			DemoState = new BuildInfo {
				Url = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/",
				Group = "Core",
				Name = "app.studio-enterprise.schema-view",
				Number = "",
				Status = BuildStatus.Failed,
				BranchName = "",
				Changes = new[] {
					new VcsChange(new VcsUser("test", "Test"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty),
					new VcsChange(new VcsUser("admin", "Admin"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty)
				},
				BuildConfigId = 8,
			}
		});
		var buildConfig9 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(jenkinsConnector.Entity, "app.studio-enterprise.process-designer") {
			DemoState = new BuildInfo {
				Url = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/",
				Group = "Core",
				Name = "app.studio-enterprise.process-designer",
				Number = "",
				Status = BuildStatus.Success,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				Changes = new[] {
					new VcsChange(new VcsUser("test", "Test"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty),
					new VcsChange(new VcsUser("admin", "Admin"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty)
				},
				BuildConfigId = 9,
			}
		});
		var buildConfig10 = await context.BuildConfigurations.AddAsync(new BuildConfigModel(jenkinsConnector.Entity, "lib.studio-enterprise.process") {
			DemoState = new BuildInfo {
				Url = "https://ts1-infr-jenkins.bpmonline.com/job/app.studio-enterprise.schema-view/job/master/9248/",
				Group = "Core",
				Name = "lib.studio-enterprise.process",
				Number = "",
				Status = BuildStatus.Success,
				StartDate = DateTime.Now.AddHours(-1),
				BranchName = "",
				Changes = new[] {
					new VcsChange(new VcsUser("test", "Test"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty),
					new VcsChange(new VcsUser("admin", "Admin"), DateTimeOffset.Now, string.Empty, ImmutableArray<FileModification>.Empty)
				},
				BuildConfigId = 10,
			}
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
			Builds = {
				
			}
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
	}
}
