using System.Text.Json;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity.Tests;

[TestFixture]
public class BuildConfigProviderTests : BaseTeamCityTest
{
	private IBuildConfigProvider _buildConfigProvider = null!;

	public override void Setup() {
		base.Setup();
		_buildConfigProvider = ServiceProvider.GetRequiredService<IBuildConfigProvider>();
	}

	[Test]
	public async Task GetAll() {
		var res = await _buildConfigProvider.GetAll();
		var expectedItems = JsonSerializer.Deserialize<BuildConfigInfo[]>(
		"""
		[
		{
			"Key": "Test1_BuildTest1",
				"Props": {
					"ProjectName": "gogs_Test1"
				}
		},
		{
			"Key": "Project2_Build",
			"Props": {
				"ProjectName": "project2"
			}
		},
		{
			"Key": "TestProject1_IntegrationTests",
			"Props": {
				"ProjectName": "testProject1"
			}
		},
		{
			"Key": "TestProject1_UnitTest",
			"Props": {
				"ProjectName": "testProject1"
			}
		}
		]
		""");
		foreach (var expected in expectedItems!) {
			res.Should().ContainEquivalentOf(expected, options => options.ComparingRecordsByMembers());
		}
	}
}