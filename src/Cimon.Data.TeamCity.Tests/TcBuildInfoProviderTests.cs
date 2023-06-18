using Cimon.Contracts;
using Cimon.Contracts.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.TeamCity.Tests;

[TestFixture]
public class TcBuildInfoProviderTests : BaseTeamCityTest
{
	private IBuildInfoProvider _buildInfoProvider = null!;

	public override void Setup() {
		base.Setup();
		_buildInfoProvider = ServiceProvider.GetRequiredService<IBuildInfoProvider>();
	}
	
	[Test]
	public async Task GetInfo() {
		var results = await _buildInfoProvider.GetInfo(new[] { new BuildConfigInfo("Test1_BuildTest1") });
		results.Should().HaveCount(1);
	}
}