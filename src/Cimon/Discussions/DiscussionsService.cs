using System.Reactive.Linq;
using Cimon.Data.Discussions;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Discussions;


[Route("discussions")]
public class DiscussionsService : Controller
{
	private readonly BuildDiscussionStoreService _discussionStore;
	public DiscussionsService(BuildDiscussionStoreService discussionStore) {
		_discussionStore = discussionStore;
	}

	[HttpGet]
	[Route("executeAction")]
	public async Task<IActionResult> ExecuteAction([FromQuery]string buildTypeId, [FromQuery]Guid actionId) {
		var discussion = await _discussionStore.GetDiscussionService(buildTypeId).FirstAsync();
		await discussion.ExecuteAction(actionId);
		return Ok();
	}
}