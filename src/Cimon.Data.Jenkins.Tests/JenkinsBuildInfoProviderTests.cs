﻿using Cimon.Contracts;
using FluentAssertions;
using FluentAssertions.Execution;

namespace Cimon.Data.Jenkins.Tests;

[TestFixture]
public class JenkinsBuildInfoProviderTests : BaseJenkinsTest
{
	private JenkinsBuildInfoProvider _provider = null!;
		
	protected override void Setup() {
		base.Setup();
		_provider = new JenkinsBuildInfoProvider(Factory);
	}

	[Test]
	public async Task GetInfo_WhenFailed() {
		using var client = Factory.Create();
		var result = await _provider.GetInfo(new BuildInfoQuery[] {
			new(new("app.my.test", null), new BuildInfoQueryOptions("old"))
		});
		var job = await client.GetJob("app.my.test", default);
		var number = job.LastBuild.Number;
		using var scope = new AssertionScope();
		var info = result.Should().ContainSingle().Subject;
		info.Url.Should().Be($"http://localhost:8080/job/app.my.test/{number}/");
		info.Name.Should().Be($"#{number}");
		info.Number.Should().Be($"{number}");
		info.BuildConfigId.Should().Be("app.my.test");
		info.StatusText.Should().Be("FAILURE");
		info.Status.Should().Be(BuildStatus.Failed);
		var commit = DateTimeOffset.Now.AddDays(-10);
		info.StartDate.Should().BeAfter(commit);
		info.EndDate.Should().BeAfter(info.StartDate);
		info.Duration.Should().BeGreaterThan(TimeSpan.FromMilliseconds(100));
		info.Committers.Should().Contain("test");
		info.Log.Should().NotBeNullOrWhiteSpace().And.Contain("exit 1");
		var change = info.Changes.Should().ContainSingle().Subject;
		change.Author.Name.Should().Be("test");
		change.Date.Should().BeBefore(info.StartDate);
		change.Date.Should().BeAfter(commit);
		change.CommitMessage.Should().Contain("Changes from");
		change.Modifications.Should().ContainEquivalentOf(new FileModification(FileModificationType.Edit, "1.txt"));
	}

	[Test]
	public async Task GetInfo_WhenMultibranch() {
		var result = await _provider.GetInfo(new BuildInfoQuery[] {
			new (new("app.my.multibranch", "master"))
		});
		result.Should().NotBeEmpty();
	}
}