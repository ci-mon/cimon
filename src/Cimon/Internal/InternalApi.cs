using Akka.Actor;
using Akka.Hosting;
using Cimon.Contracts.CI;
using Cimon.Contracts.Services;
using Cimon.Data;
using Cimon.Data.DemoData;
using Cimon.Data.Monitors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Internal;

[Route("api/internal")]
[Authorize(Policy = "LocalhostPolicy")]
public class InternalApiController(IServiceProvider serviceProvider) : ControllerBase
{
	private readonly IRequiredActor<MonitorServiceActor> _monitorServiceActor =
		serviceProvider.GetRequiredService<IRequiredActor<MonitorServiceActor>>();

	[HttpPost]
	[Route("emulateAllBuildsAreGreen")]
	public IActionResult EmulateAllBuildsState([FromQuery] bool? value) {
		DemoBuildInfoProvider.SetStateForAll(value);
		_monitorServiceActor.ActorRef.Tell(new ActorsApi.RefreshAllMonitors());
		return Ok();
	}

	[HttpPost]
	public IActionResult SetBuildState([FromQuery] string buildConfigKey, [FromBody] DemoBuildInfo demoBuildInfo) {
		DemoBuildInfoProvider.SetBuildState(buildConfigKey, demoBuildInfo);
		_monitorServiceActor.ActorRef.Tell(new ActorsApi.RefreshAllMonitors());
		return Ok();
	}
}
