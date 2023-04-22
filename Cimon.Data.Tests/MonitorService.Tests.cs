using System.Reactive.Linq;

namespace Cimon.Data.Tests;

public class MonitorServiceTests
{
	[Test]
	public async Task Get() {
		var sut = new MonitorService();
		var observable = sut.GetMonitors();
		for (int i = 0; i < 5; i++) {
			var monitors = await observable.FirstOrDefaultAsync();
			monitors.Should().HaveCountGreaterThanOrEqualTo(1);
		}
	}

	[Test]
	public async Task Update() {
		var sut = new MonitorService();
		var observable = sut.GetMonitors();
		var monitors = await observable.FirstAsync();
		var monitor = monitors[0];
		var id = Guid.NewGuid().ToString();
		monitor.Id = id;
		await sut.Save(monitor);
		monitors = await observable.FirstOrDefaultAsync();
		monitors.Should().Contain(m => m.Id == id);
	}
}