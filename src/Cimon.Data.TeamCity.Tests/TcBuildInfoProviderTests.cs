using Cimon.Contracts;
using Cimon.Contracts.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using TeamCityAPI.Locators;
using TeamCityAPI.Queries;

namespace Cimon.Data.TeamCity.Tests;

using Cimon.Contracts.CI;
using Cimon.Data.ML;

[TestFixture]
public class TcBuildInfoProviderTests : BaseTeamCityTest
{
	private TcBuildInfoProvider _buildInfoProvider = null!;
	private TcClientFactory _clientFactory = null!;

	public override void Setup() {
		base.Setup();
		_buildInfoProvider = (ServiceProvider.GetRequiredService<IBuildInfoProvider>() as TcBuildInfoProvider)!;
		_clientFactory = ServiceProvider.GetRequiredService<TcClientFactory>();
	}

	[Test]
	public async Task Debug() {
		var info = await _buildInfoProvider.GetSingleBuildInfo("DotNetUnitTests", 5738602);
		info.Should().NotBeNull();
	}

	[Test]
	public async Task GetInfo_WhenFailed() {
		var results = await _buildInfoProvider.GetInfo(new BuildInfoQuery[]
			{ new(new("Test1_BuildTest1", null, true), new BuildInfoQueryOptions("old")) });
		using var client = _clientFactory.GetClient();
		var lastBuild = await client.Client.Builds.Include(x=>x.Build).WithLocator(new BuildLocator {
			BuildType = new BuildTypeLocator {
				Id = "Test1_BuildTest1"
			},
			Branch = new BranchLocator {
				Name = "master"
			}
		}).GetAsync(1);
		var build = lastBuild.Build.First();
		using var scope = new AssertionScope();
		var info = results.Should().ContainSingle().Subject;
		info.Url.Should().Be($"http://localhost:8111/viewLog.html?buildId={build.Id}&buildTypeId=Test1_BuildTest1");
		info.Name.Should().Be($"Build test1");
		info.Number.Should().Be($"{build.Number}");
		info.BranchName.Should().Be("master");
		info.Group.Should().Be("gogs_Test1");
		info.BuildConfigId.Should().Be("Test1_BuildTest1");
		info.StatusText.Should().Be("Exit code 1 (Step: Command Line) (new)");
		info.Status.Should().Be(BuildStatus.Failed);
		var commit = DateTimeOffset.Now.AddDays(-10);
		info.StartDate.Should().BeAfter(commit);
		info.EndDate.Should().BeAfter(info.StartDate.Value);
		info.Duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
		var change = info.Changes.Should().ContainSingle().Subject;
		change.Author.Name.Should().Be("test");
		change.Date.Should().BeBefore(info.StartDate.Value);
		change.Date.Should().BeAfter(commit);
		change.CommitMessage.Should().Contain("Changes from");
		change.Modifications.Should().ContainEquivalentOf(new FileModification(FileModificationType.Edit, "1.txt"));
		info.Log.Should().NotBeNullOrWhiteSpace().And.Contain("Process exited with code 1");
	}
}