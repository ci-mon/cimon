using System.Text.Json;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity.Tests;

using Cimon.Contracts.CI;

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
		var res = await _buildConfigProvider.GetAll();
		
	}

	[Test]
	public async Task GetAll() {
		var res = await _buildConfigProvider.GetAll();
		using var scope = new AssertionScope();
		res.Should().ContainEquivalentOf(new BuildConfigInfo("Test1_BuildTest1", "master", true) {
				Props = {
					{"ProjectId", "gogs_Test1"}
				}
			}, 
			options => options.ComparingRecordsByMembers());
		res.Should().ContainEquivalentOf(new BuildConfigInfo("Test1_BuildTest1", "test2"){
				Props = {
					{"ProjectId", "gogs_Test1"}
				}
			}, 
			options => options.ComparingRecordsByMembers());
		res.Should().ContainEquivalentOf(new BuildConfigInfo("TestProject1_IntegrationTests", null, true) {
				Props = {
					{"ProjectId", "testProject1"}
				}
			}, 
			options => options.ComparingRecordsByMembers());
	}
}