using System.Reactive.Linq;
using Cimon.Contracts;
using Cimon.Contracts.Services;
using Cimon.Data.BuildInformation;
using Cimon.Data.Discussions;
using Cimon.Data.Users;
using Cimon.DB.Models;

namespace Cimon.Data.Tests;

using System.Reactive.Subjects;
using Cimon.Contracts.CI;
using Microsoft.Extensions.Options;
using NSubstitute;

public class BuildInfoServiceTests
{

	private BuildInfoService _service;
	private List<IBuildInfoProvider> _buildInfoProviders;
	private IBuildInfoProvider _buildInfoProvider;
	private BuildConfig _sampleBuildLocator1;
	private BuildConfig _sampleBuildLocator2;
	private Subject<long> _timer;
	private BuildDiscussionStoreService _buildDiscussionStoreService;

	private BuildInfo CreateBuildInfo(string name, string id) {
		return new BuildInfo() {
			Name = "name",
			BuildConfigId = id,
			Url = "",
			Group = "",
			Number = "",
			StatusText = "",
			BranchName = "",
		};
	}

	[SetUp]
	public void Setup() {
		var options = Options.Create(new BuildInfoMonitoringSettings() {
			Delay = TimeSpan.FromMilliseconds(100)
		});
		_buildInfoProviders = new List<IBuildInfoProvider>();
		_buildInfoProvider = Substitute.For<IBuildInfoProvider>();
		_buildInfoProviders.Add(_buildInfoProvider);
		_timer = new Subject<long>();
		var notificationService = Substitute.For<INotificationService>();
		_buildDiscussionStoreService = new BuildDiscussionStoreService(notificationService);
		_service = new BuildInfoService(options.Value, _buildInfoProviders, _buildDiscussionStoreService, 
			Substitute.For<IBuildMonitoringService>(), span => _timer);
		_sampleBuildLocator1 = new BuildConfig(CISystem.TeamCity, "testId1");
		_sampleBuildLocator2 = new BuildConfig(CISystem.TeamCity, "testId2");
		_buildInfoProvider.CiSystem.Returns(CISystem.TeamCity);
	}

	[Test]
	public async Task Watch_UpdateByTimer() {
		_buildInfoProvider.GetInfo(null!)
			.ReturnsForAnyArgs(ci => {
				var locators = ci.Arg<IEnumerable<BuildConfig>>();
				var buildInfos = locators.Reverse().Select(l => CreateBuildInfo("Test build", l.Key)).ToList();
				return Task.FromResult((IReadOnlyCollection<BuildInfo>)buildInfos);
			});
		var locators = new BehaviorSubject<List<BuildConfig>>(new List<BuildConfig> {
			_sampleBuildLocator1,
			_sampleBuildLocator2
		});
		var items = _service.Watch(locators);
		IList<BuildInfo>? infos = null;
		IList<BuildInfo>? infosForOtherSubscriber = null;
		using (items.Subscribe(x => infos = x)) {
			await Wait.ForAssert(() => infos.Should().HaveCount(2).And.ContainInOrder(
				CreateBuildInfo("Test build", _sampleBuildLocator1.Key),
				CreateBuildInfo("Test build", _sampleBuildLocator2.Key)));
			using (items.Subscribe(x => infosForOtherSubscriber = x)) {
				_timer.OnNext(1);
				await Wait.ForAssert(() => infos.Should().HaveCount(2));
				await Wait.ForAssert(() => infosForOtherSubscriber.Should().HaveCount(2));
			}
			locators.OnNext(new List<BuildConfig>());
			await Wait.ForAssert(() => infos.Should().HaveCount(0));
		}
		infos = null;
		locators.OnNext(new List<BuildConfig>());
		await Wait.ForConditionNotChanged(() => infos.Should().BeNull());
		infosForOtherSubscriber.Should().HaveCount(2);
		_buildInfoProvider.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IBuildInfoProvider.GetInfo))
			.Should().HaveCount(2);
	}

	[Test]
	public async Task Watch_UpdateCommentsInfo() {
		_buildInfoProvider.GetInfo(null!)
			.ReturnsForAnyArgs(ci => {
				var locators = ci.Arg<IEnumerable<BuildConfig>>();
				var buildInfos = locators.Reverse().Select(l => CreateBuildInfo("Test build", l.Key)).ToList();
				return Task.FromResult((IReadOnlyCollection<BuildInfo>)buildInfos);
			});
		var locators = new BehaviorSubject<List<BuildConfig>>(new List<BuildConfig> {
			_sampleBuildLocator1,
		});
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator1.Key);
		var discussionService = await _buildDiscussionStoreService.GetDiscussionService(_sampleBuildLocator1.Key)
			.FirstAsync();
		var items = _service.Watch(locators);
		var info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(0);
		await discussionService.AddComment(new CommentData());
		info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(1);
		_timer.OnNext(1);
		info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(1);
		await _buildDiscussionStoreService.CloseDiscussion(_sampleBuildLocator1.Key);
		info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(0);
	}

	[Test]
	public async Task Watch_SubscribeAndUnsubscribe() {
		_buildInfoProvider.GetInfo(null!)
			.ReturnsForAnyArgs(ci => {
				var locators = ci.Arg<IEnumerable<BuildConfig>>();
				var buildInfos = locators.Reverse().Select(l => CreateBuildInfo("Test build" + l.Key, l.Key)).ToList();
				return Task.FromResult((IReadOnlyCollection<BuildInfo>)buildInfos);
			});
		var locators = new BehaviorSubject<List<BuildConfig>>(new List<BuildConfig> {
			_sampleBuildLocator1,
			_sampleBuildLocator2
		});
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator1.Key);
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator2.Key);
		await AddComment(_sampleBuildLocator1.Key);
		await AddComment(_sampleBuildLocator2.Key);
		var stream = _service.Watch(locators).ToAsyncEnumerable().GetAsyncEnumerator();
		async Task<IList<BuildInfo>> GetNextAsync() {
			await stream.MoveNextAsync();
			return stream.Current;
		}
		async Task WaitItem(Func<BuildInfo, bool> info) {
			await Wait.ForAssert(async () => {
				var current = await GetNextAsync();
				current.Should().Contain(x => info(x));
			});
		}
		var current = await GetNextAsync();
		current.Should().Contain(x => x.CommentsCount == 1);
		await _buildDiscussionStoreService.CloseDiscussion(_sampleBuildLocator1.Key);
		current = await GetNextAsync();
		current.Should().Contain(x => x.CommentsCount == 0 && x.BuildConfigId == _sampleBuildLocator1.Key);
		await _buildDiscussionStoreService.CloseDiscussion(_sampleBuildLocator2.Key);
		current = await GetNextAsync();
		current.Should().Contain(x => x.CommentsCount == 0);
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator1.Key);
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator2.Key);
		await AddComment(_sampleBuildLocator1.Key);
		await WaitItem(x => x.BuildConfigId == _sampleBuildLocator1.Key && x.CommentsCount == 1);
		await AddComment(_sampleBuildLocator1.Key);
		await WaitItem(x => x.BuildConfigId == _sampleBuildLocator1.Key && x.CommentsCount == 2);
		for (int i = 1; i <= 3; i++) {
			await AddComment(_sampleBuildLocator1.Key);
			var expectedComments = 2 + i;
			await WaitItem(x => x.BuildConfigId == _sampleBuildLocator1.Key && x.CommentsCount == expectedComments);
			_timer.OnNext(1);
			await WaitItem(x => x.BuildConfigId == _sampleBuildLocator1.Key && x.CommentsCount == expectedComments);
		}
	}

	private async Task AddComment(string buildId) {
		var discussionService = await _buildDiscussionStoreService.GetDiscussionService(buildId)
			.FirstAsync();
		await discussionService.AddComment(new CommentData());
	}
}
