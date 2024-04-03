using Cimon.Contracts.Services;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Cimon.Data.Jenkins.Tests;

using Cimon.Data.BuildInformation;
using Cimon.Jenkins;
using Contracts.CI;

[TestFixture]
public class JenkinsBuildInfoProviderTests : BaseJenkinsTest
{

	[Test]
	public async Task GetInfo_WhenFailed() {
		using var client = Factory.Create(DefaultConnector.ConnectorKey);
		var query = new BuildInfoQuery(DefaultConnector, new BuildConfig {
			Key = "app.my.test",
			Branch = "master"
		}, new BuildInfoQueryOptions(null, 5));
		var info = (await BuildInfoProvider.FindInfo(query)).First();
		var job = await client.Query(new JenkinsApi.Job("app.my.test"));
		var number = job.LastBuild.Number;
		using var scope = new AssertionScope();
		info!.Url.Should().Be($"http://localhost:8080/job/app.my.test/{number}/");
		info.Name.Should().Be($"#{number}");
		info.Id.Should().Be($"{number}");
		info.StatusText.Should().Be("FAILURE");
		info.Status.Should().Be(BuildStatus.Failed);
		var commit = DateTimeOffset.Now.AddDays(-10);
		info.StartDate.Should().BeAfter(commit);
		info.EndDate.Should().BeAfter(info.StartDate!.Value);
		info.Duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
		info.Log.Should().NotBeNullOrWhiteSpace().And.Contain("exit 1");
		var change = info.Changes.Should().ContainSingle().Subject;
		change.Date.Should().BeBefore(info.StartDate.Value);
		change.Date.Should().BeAfter(commit);
		change.CommitMessage.Should().Contain("Changes from");
		change.Modifications.Should().ContainEquivalentOf(new FileModification(FileModificationType.Edit, "1.txt"));
	}

	[Test]
	public async Task AddInfoToHistory() {
		var query = new BuildInfoQuery(DefaultConnector, new BuildConfig {
			Key = "app.studio-enterprise.shell",
			Branch = "master"
		}, new BuildInfoQueryOptions(null, 5));
		var info = await BuildInfoProvider.FindInfo(query);
		var history = new BuildInfoHistory();
		foreach (var result in info) {
			history.Add(result);
		}
	}

	[Test]
	public async Task GetInfo_WhenMultibranch() {
		var result = await BuildInfoProvider.FindInfo(new BuildInfoQuery(DefaultConnector, new BuildConfig {
			Key = "app.my.multibranch",
			Branch = "master"
		}, new BuildInfoQueryOptions(null, 1)));
		result.Should().NotBeNull();
	}
}
