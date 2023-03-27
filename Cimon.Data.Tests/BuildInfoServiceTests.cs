namespace Cimon.Data.Tests;

using System.Reactive.Linq;

public class BuildInfoServiceTests
{
	private BuildInfoService _service;

	[SetUp]
	public void Setup() {
		_service = new BuildInfoService();
	}

	[Test]
	public async Task Test1() {
		bool stop = false;
		IObservable<IList<BuildInfo>> items = _service.Watch(new List<BuildLocator> {
			new BuildLocator {
				Id = "testId",
				CiSystem = CISystem.TeamCity
			}
		});
		(await items.FirstAsync()).Should().HaveCount(0);
		_service.OnTcStatusChange();
		(await items.FirstAsync()).Should().HaveCount(1).And.ContainEquivalentOf(new BuildInfo {
			Name = "test",
			BuildId = "testId"
		});
		_service.Stop();
	}
}
