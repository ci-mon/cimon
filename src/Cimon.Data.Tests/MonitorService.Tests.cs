using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cimon.Contracts;
using Cimon.Data.BuildInformation;
using Monitor = Cimon.DB.Models.Monitor;

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
		monitor.Key = id;
		await sut.Save(monitor);
		monitors = await observable.FirstOrDefaultAsync();
		monitors.Should().Contain(m => m.Key == id);
	}

	[Test]
	public async Task Add() {
		var sut = new MonitorService();
		var monitors = await sut.GetMonitors().FirstAsync();
		var newMon = await sut.Add();
		monitors = await sut.GetMonitors().FirstAsync();
		monitors.Should().Contain(x=>x.Key == newMon.Key);
	}

	[Test]
	public async Task GetMonitors_MultipleSubscribe() {
		var sut = new MonitorService();
		var task = Enumerable.Range(0, 10).Select(x => sut.GetMonitors()).Select(async x => await x.FirstAsync())
			.ToArray();
		var monitors = await Task.WhenAll(task);
		monitors.Should().HaveCount(10);
	}

	[Test]
	public async Task GetMonitorById_MultipleSubscribe() {
		var sut = new MonitorService();
		var expected = MockData.Monitors.First();
		for (int i = 0; i < 10; i++) {
			var tcs = new TaskCompletionSource<Monitor>();
			var disposed = new Subject<bool>();
			sut.GetMonitorById(expected.Key).TakeUntil(disposed).Subscribe(x => tcs.SetResult(x));
			var result = await tcs.Task;
			//disposed.OnNext(true);
			result.Key.Should().BeSameAs(expected.Key);
		}
	}
}