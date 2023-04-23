using System.Reactive.Linq;
using NSubstitute;

namespace Cimon.Data.Tests;

public class ReactiveRepositoryTests
{
	[Test]
	public async Task Items() {
		var api = Substitute.For<IReactiveRepositoryApi<string>>();
		api.LoadData(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string>("hello"));
		var sut = new ReactiveRepository<string>(api);
		api.ReceivedCalls().Should().BeEmpty();
		for (int i = 0; i < 5; i++) {
			(await sut.Items.FirstAsync()).Should().Be("hello");
		}
		api.ReceivedCalls().Should().HaveCount(1);
	}

	[Test]
	public async Task Items_MultipleSubscription() {
		var api = Substitute.For<IReactiveRepositoryApi<string>>();
		api.LoadData(Arg.Any<CancellationToken>()).Returns(Task.FromResult<string>("hello"));
		var sut = new ReactiveRepository<string>(api);
		var results = await Task.WhenAll(Enumerable.Repeat(0, 5).Select(x => {
			var tcs = new TaskCompletionSource<string>();
			sut.Items.Subscribe(x => tcs.SetResult(x));
			return tcs;
		}).Select(async x => await x.Task).ToArray()).WaitAsync(TimeSpan.FromSeconds(1));
		results.Should().HaveCount(5);
	}

}