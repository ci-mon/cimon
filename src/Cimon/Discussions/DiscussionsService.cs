using Akka.Hosting;
using Cimon.Data;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Discussions;

[Route("discussions")]
public class DiscussionsService : Controller
{
	[HttpGet]
	[Route("executeAction")]
	public async Task<IActionResult> ExecuteAction([FromQuery]int buildTypeId, [FromQuery]Guid actionId, 
			[FromServices]IRequiredActor<DiscussionStoreActor> discussionStore) {
		var discussionHandle = await discussionStore.ActorRef.Ask(new ActorsApi.FindDiscussion(buildTypeId));
		discussionHandle.ExecuteAction(actionId);
		return Ok();
	}
}