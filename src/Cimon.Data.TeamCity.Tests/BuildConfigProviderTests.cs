using Cimon.Contracts.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity.Tests;

using Contracts.CI;

[TestFixture]
public class BuildConfigProviderTests : BaseTeamCityTest
{

	[Test]
	public async Task Debug() {
		var res = await BuildConfigProvider.GetAll(DefaultConnector, null);
	}

	[Test]
	public async Task GetAll() {
		var res = await BuildConfigProvider.GetAll(DefaultConnector, null);
		using var scope = new AssertionScope();
		res.Should().ContainEquivalentOf(new BuildConfig {
				Name = "Test1_BuildTest1",
				Props = {
					{"ProjectId", "gogs_Test1"}
				}
			},
			options => options.ComparingRecordsByMembers());
		res.Should().ContainEquivalentOf(new BuildConfig{
				Props = {
					{"ProjectId", "gogs_Test1"}
				}
			},
			options => options.ComparingRecordsByMembers());
		res.Should().ContainEquivalentOf(new BuildConfig {
				Props = {
					{"ProjectId", "testProject1"}
				}
			},
			options => options.ComparingRecordsByMembers());
	}
}
