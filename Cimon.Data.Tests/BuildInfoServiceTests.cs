namespace Cimon.Data.Tests;

using System.Reactive.Linq;
using Microsoft.Extensions.Options;
using NSubstitute;

public class BuildInfoServiceTests
{

	private BuildInfoService _service;
	private List<IBuildInfoProvider> _buildInfoProviders;
	private IBuildInfoProvider _buildInfoProvider;
	private BuildLocator _sampleBuildLocator;

	[SetUp]
	public void Setup() {
		var options = Options.Create(new BuildInfoMonitoringSettings() {
			Delay = TimeSpan.FromMilliseconds(100)
		});
		_buildInfoProviders = new List<IBuildInfoProvider>();
		var buildInfoProvider = Substitute.For<IBuildInfoProvider>();
		_buildInfoProvider = buildInfoProvider;
		_buildInfoProviders.Add(_buildInfoProvider);
		_service = new BuildInfoService(options, _buildInfoProviders);
		_sampleBuildLocator = new BuildLocator {
			Id = "testId",
			CiSystem = CISystem.TeamCity
		};
	}

	[Test]
	public async Task Test1() {
		BuildInfo testBuildInfo = new() {
			Name = "Test build",
			BuildId = _sampleBuildLocator.Id
		};
		_buildInfoProvider.GetInfo(Arg.Any<IList<BuildLocator>>()).Returns(new List<BuildInfo> {
			testBuildInfo
		});
		IObservable<IList<BuildInfo>> items = _service.Watch(new List<BuildLocator> { _sampleBuildLocator });
		_service.IsRunning.Should().BeFalse();
		IList<BuildInfo> infos = null;
		using (items.Subscribe(x => infos = x)) {
			_service.IsRunning.Should().BeTrue();
			await TestUtils.WaitFor(() => infos != null);
			infos.Should().HaveCount(0);
			await TestUtils.WaitForAssert(() => infos.Should().HaveCount(1).And.ContainEquivalentOf(testBuildInfo));
		}
		_service.IsRunning.Should().BeFalse();
	}
}
