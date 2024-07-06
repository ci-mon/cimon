using Cimon.Contracts.Services;
using Cimon.Data.BuildInformation;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.FeatureManagement;
using NSubstitute;
using TeamCityAPI.Locators;
using TeamCityAPI.Queries;

namespace Cimon.Data.TeamCity.Tests;

using Contracts.CI;
using Microsoft.Extensions.Options;
using ML;

[TestFixture]
public class TcBuildInfoProviderTests : BaseTeamCityTest
{
	private TcClientFactory _clientFactory = null!;

	protected override void Setup() {
		base.Setup();
		_clientFactory = ServiceProvider.GetRequiredService<TcClientFactory>();
	}

	[Test]
	public async Task Debug() {
		var info = await BuildInfoProvider.GetSingleBuildInfo("DotNetUnitTests", 5652629,
			DefaultConnector.ConnectorKey);//5711671,5738602
		info.Should().NotBeNull();
		var utils = new BuildFailurePredictor(Substitute.For<IFeatureManager>());
		var author = await utils.FindFailureSuspects(info, false);
		author.Should().NotBeNull();
	}

	[Test]
	public async Task GetInfo_ShouldLoadFullHistory_WhenLastBuildNumberNotSpecified() {
		var buildConfig = new BuildConfig {
			Key = "Test1_BuildConf1"
		};
		var query = new BuildInfoQuery(DefaultConnector, buildConfig, new BuildInfoQueryOptions(null, 5, 5));
		var results = await BuildInfoProvider.FindInfo(query);
		var history = new BuildInfoHistory();
		foreach (var result in results) {
			history.Add(result);
		}
		history.CombinedInfo.Should().NotBeNull();
	}

	[Test]
	public async Task GetInfo_WhenFailed() {
		var buildConfig = new BuildConfig {
			Key = "Test1_BuildConf1"
		};
		var query = new BuildInfoQuery(DefaultConnector, buildConfig, new BuildInfoQueryOptions("0", 1, 1));
		var results = await BuildInfoProvider.FindInfo(query);
		using var client = _clientFactory.Create(DefaultConnector.ConnectorKey);
		var lastBuild = await client.Client.Builds.Include(x=>x.Build).WithLocator(new BuildLocator {
			BuildType = new BuildTypeLocator {
				Id = buildConfig.Key
			},
			Branch = new BranchLocator {
				Name = "refs/heads/master"
			}
		}).GetAsync(1);
		var build = lastBuild.Build.First();
		using var scope = new AssertionScope();
		var info = results.Should().ContainSingle().Subject;
		info.Url.Should().Be($"http://localhost:8111/viewLog.html?buildId={build.Id}&buildTypeId=Test1_BuildTest1");
		info.Name.Should().Be($"Build test1");
		info.Id.Should().Be($"{build.Number}");
		info.BranchName.Should().Be("refs/heads/master");
		info.Group.Should().Be("gogs_Test1");
		info.StatusText.Should().Be("Exit code 1 (Step: Command Line) (new)");
		info.Status.Should().Be(BuildStatus.Failed);
		var commit = DateTimeOffset.Now.AddDays(-10);
		info.StartDate.Should().BeAfter(commit);
		info.EndDate.Should().BeAfter(info.StartDate!.Value);
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
