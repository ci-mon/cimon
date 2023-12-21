using Cimon.Contracts.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity.Tests;

using Contracts.CI;

[TestFixture]
public class BuildConfigProviderTests : BaseTeamCityTest
{
	private IBuildConfigProvider _buildConfigProvider = null!;

	public override void Setup() {
		base.Setup();
		_buildConfigProvider = ServiceProvider.GetRequiredService<IBuildConfigProvider>();
	}

	[Test]
	public async Task Debug() {
		var res = await _buildConfigProvider.GetAll(EmptyCIConnectorInfo);
	}

	private static CIConnectorInfo EmptyCIConnectorInfo => new("key", new Dictionary<string, string>());

	[Test]
	public async Task GetAll() {
		var res = await _buildConfigProvider.GetAll(EmptyCIConnectorInfo);
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
