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
	public async Task<IActionResult> ExecuteAction([FromQuery]int buildTypeId, [FromQuery]Guid actionId) {
		var discussionHandle = await AppActors.Instance.DiscussionsService.Ask(new ActorsApi.FindDiscussion(buildTypeId));
		discussionHandle.ExecuteAction(actionId);
		return Ok();
	}
}