namespace Cimon.Data.Tests;

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Options;
using NSubstitute;

public class BuildInfoServiceTests
{

	private BuildInfoService _service;
	private List<IBuildInfoProvider> _buildInfoProviders;
	private IBuildInfoProvider _buildInfoProvider;
	private BuildLocator _sampleBuildLocator1;
	private BuildLocator _sampleBuildLocator2;

	[SetUp]
	public void Setup() {
		var options = Options.Create(new BuildInfoMonitoringSettings() {
			Delay = TimeSpan.FromMilliseconds(100)
		});
		_buildInfoProviders = new List<IBuildInfoProvider>();
		_buildInfoProvider = Substitute.For<IBuildInfoProvider>();
		_buildInfoProviders.Add(_buildInfoProvider);
		_service = new BuildInfoService(options, _buildInfoProviders);
		_sampleBuildLocator1 = new BuildLocator {
			Id = "testId1",
			CiSystem = CISystem.TeamCity
		};
		_sampleBuildLocator2 = new BuildLocator {
			Id = "testId2",
			CiSystem = CISystem.TeamCity
		};
		_buildInfoProvider.CiSystem.Returns(CISystem.TeamCity);
	}

	[Test]
	public async Task Test1() {
		_buildInfoProvider.GetInfo(null)
			.ReturnsForAnyArgs(ci => {
				var locators = ci.Arg<IEnumerable<BuildLocator>>();
				var buildInfos = locators.Reverse().Select(l => new BuildInfo {
					Name = "Test build",
					BuildId = l.Id
				}).ToList();
				return Task.FromResult((IList<BuildInfo>)buildInfos);
			});
		var locators = new BehaviorSubject<List<BuildLocator>>(new List<BuildLocator> {
			_sampleBuildLocator1,
			_sampleBuildLocator2
		});
		_service.IsRunning.Should().BeFalse();
		var items = _service.Watch(locators);
		IList<BuildInfo> infos = null;
		using (items.Subscribe(x => infos = x)) {
			_service.IsRunning.Should().BeTrue();
			await Wait.ForAssert(() => infos.Should().HaveCount(2).And.ContainInOrder(new BuildInfo {
				Name = "Test build",
				BuildId = _sampleBuildLocator1.Id
			}, new BuildInfo {
				Name = "Test build",
				BuildId = _sampleBuildLocator2.Id
			}));
			locators.OnNext(new List<BuildLocator>());
			await Wait.ForAssert(() => infos.Should().HaveCount(0));
		}
		_service.IsRunning.Should().BeFalse();
	}

	[Test]
	public async Task METHOD() {
		var x1 = new BehaviorSubject<int>(1).Select(i => {
			Console.WriteLine(i);
			return i;
		}).Replay(x=>x);
		var v1 = await x1.FirstAsync();
	}
}
