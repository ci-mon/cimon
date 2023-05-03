using System.Reactive.Linq;

namespace Cimon.Data.Tests;

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
	private Subject<long> _timer;
	private BuildDiscussionStoreService _buildDiscussionStoreService;

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
		_service = new BuildInfoService(options, _buildInfoProviders, _buildDiscussionStoreService, 
			Substitute.For<IBuildMonitoringService>(), span => _timer);
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
	public async Task Watch_UpdateByTimer() {
		_buildInfoProvider.GetInfo(null)
			.ReturnsForAnyArgs(ci => {
				var locators = ci.Arg<IEnumerable<BuildLocator>>();
				var buildInfos = locators.Reverse().Select(l => new BuildInfo {
					Name = "Test build",
					BuildId = l.Id
				}).ToList();
				return Task.FromResult((IReadOnlyCollection<BuildInfo>)buildInfos);
			});
		var locators = new BehaviorSubject<List<BuildLocator>>(new List<BuildLocator> {
			_sampleBuildLocator1,
			_sampleBuildLocator2
		});
		var items = _service.Watch(locators);
		IList<BuildInfo>? infos = null;
		IList<BuildInfo>? infosForOtherSubscriber = null;
		using (items.Subscribe(x => infos = x)) {
			await Wait.ForAssert(() => infos.Should().HaveCount(2).And.ContainInOrder(new BuildInfo {
				Name = "Test build",
				BuildId = _sampleBuildLocator1.Id
			}, new BuildInfo {
				Name = "Test build",
				BuildId = _sampleBuildLocator2.Id
			}));
			using (items.Subscribe(x => infosForOtherSubscriber = x)) {
				_timer.OnNext(1);
				await Wait.ForAssert(() => infos.Should().HaveCount(2));
				await Wait.ForAssert(() => infosForOtherSubscriber.Should().HaveCount(2));
			}
			locators.OnNext(new List<BuildLocator>());
			await Wait.ForAssert(() => infos.Should().HaveCount(0));
		}
		infos = null;
		locators.OnNext(new List<BuildLocator>());
		await Wait.ForConditionNotChanged(() => infos.Should().BeNull());
		infosForOtherSubscriber.Should().HaveCount(2);
		_buildInfoProvider.ReceivedCalls().Where(c => c.GetMethodInfo().Name == nameof(IBuildInfoProvider.GetInfo))
			.Should().HaveCount(2);
	}

	[Test]
	public async Task Watch_UpdateCommentsInfo() {
		_buildInfoProvider.GetInfo(null)
			.ReturnsForAnyArgs(ci => {
				var locators = ci.Arg<IEnumerable<BuildLocator>>();
				var buildInfos = locators.Reverse().Select(l => new BuildInfo {
					Name = "Test build",
					BuildId = l.Id
				}).ToList();
				return Task.FromResult((IReadOnlyCollection<BuildInfo>)buildInfos);
			});
		var locators = new BehaviorSubject<List<BuildLocator>>(new List<BuildLocator> {
			_sampleBuildLocator1,
		});
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator1.Id);
		var discussionService = await _buildDiscussionStoreService.GetDiscussionService(_sampleBuildLocator1.Id)
			.FirstAsync();
		var items = _service.Watch(locators);
		var info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(0);
		await discussionService.AddComment(new CommentData());
		info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(1);
		await _buildDiscussionStoreService.CloseDiscussion(_sampleBuildLocator1.Id);
		info = await items.FirstAsync();
		info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(0);
		var discussionServiceSub = _buildDiscussionStoreService.GetDiscussionService(_sampleBuildLocator1.Id);
		await _buildDiscussionStoreService.OpenDiscussion(_sampleBuildLocator1.Id);
		discussionService = await discussionServiceSub.FirstAsync();
		for (int i = 1; i <= 3; i++) {
			await discussionService.AddComment(new CommentData());
			items = _service.Watch(locators);
			info = await items.FirstAsync();
			info.Should().HaveCount(1).And.Subject.First().CommentsCount.Should().Be(i);
		}
	}


}
