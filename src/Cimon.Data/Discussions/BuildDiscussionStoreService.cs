using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cimon.Data.Users;
using Optional;

namespace Cimon.Data.Discussions;

public class BuildDiscussionStoreService
{
	private readonly INotificationService _notificationService;

	public BehaviorSubject<ImmutableList<BuildDiscussionService>> AllDiscussions { get; } =
		new(ImmutableList<BuildDiscussionService>.Empty);

	public BuildDiscussionStoreService(INotificationService notificationService) {
		_notificationService = notificationService;
	}

	public IObservable<IBuildDiscussionService> GetDiscussionService(string buildId) {
		return AllDiscussions.SelectMany(x=>x).Where(s=>s.BuildId == buildId);
	}

	public async Task<Option<BuildDiscussionService>> OpenDiscussion(string buildId) {
		var currentDiscussions = await AllDiscussions.FirstAsync();
		if (currentDiscussions.Any(x => x.BuildId == buildId)) {
			return Option.None<BuildDiscussionService>();
		}
		var service = new BuildDiscussionService(buildId, _notificationService);
		AllDiscussions.OnNext(currentDiscussions.Add(service));
		return service.Some();
	}

	public async Task CloseDiscussion(string buildId) {
		var currentDiscussions = await AllDiscussions.FirstAsync();
		var exiting = currentDiscussions.Find(x => x.BuildId == buildId);
		if (exiting == null) {
			return;
		}
		await exiting.Close();
		AllDiscussions.OnNext(currentDiscussions.Remove(exiting));
	}
}
