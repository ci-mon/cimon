using System.Reactive.Linq;
using Cimon.Data.BuildInformation;
using Microsoft.EntityFrameworkCore;
using Monitor = Cimon.DB.Models.Monitor;

namespace Cimon.Data.Tests;

public class MonitorServiceTests
{
	[Test]
	public async Task Get() {
		var factory = new TestDbContextFactory();
		factory.Context.Monitors.Add(new Monitor() {
			Key = "test"
		});
		await factory.Context.SaveChangesAsync();
		var sut = new MonitorService(factory);
		var observable = sut.GetMonitors();
		for (int i = 0; i < 5; i++) {
			var monitors = await observable.FirstOrDefaultAsync();
			monitors.Should().HaveCountGreaterThanOrEqualTo(1);
		}
	}

	[Test]
	public async Task Update() {
		var sut = new MonitorService(TestDbContextFactory.New);
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
		var sut = new MonitorService(TestDbContextFactory.New);
		var monitors = await sut.GetMonitors().FirstAsync();
		var newMon = await sut.Add();
		monitors = await sut.GetMonitors().FirstAsync();
		monitors.Should().Contain(x=>x.Key == newMon.Key);
	}

	[Test]
	public async Task GetMonitors_MultipleSubscribe() {
		var sut = new MonitorService(TestDbContextFactory.New);
		var task = Enumerable.Range(0, 10).Select(x => sut.GetMonitors()).Select(async x => await x.FirstAsync())
			.ToArray();
		var monitors = await Task.WhenAll(task);
		monitors.Should().HaveCount(10);
	}

	[Test]
	public async Task GetMonitorById_MultipleSubscribe() {
		var factory = new TestDbContextFactory();
		factory.Context.Monitors.Add(new Monitor() {
			Key = "test"
		});
		await factory.Context.SaveChangesAsync();
		var sut = new MonitorService(factory);
		for (int i = 0; i < 10; i++) {
			var result = await sut.GetMonitorById("test").Timeout(TimeSpan.FromSeconds(5)).FirstAsync();
			result.Key.Should().BeSameAs("test");
		}
	}
}
