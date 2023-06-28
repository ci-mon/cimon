using Cimon.Contracts;
using Cimon.Data.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Users;

[Route("api/users")]
public class UsersController : Controller
{
	private readonly UserManager _userManager;
	private readonly IMediator _mediator;
	public UsersController(UserManager userManager, IMediator mediator) {
		_userManager = userManager;
		_mediator = mediator;
	}

	[Route("search")]
	[HttpGet]
	public IAsyncEnumerable<UserInfo> Search([FromQuery]string? searchTerm) => _userManager.GetUsers(searchTerm);

	[Route("openLastMonitor")]
	[HttpGet]
	public async Task<IActionResult> OpenLastMonitor([FromQuery(Name = "full-screen")]bool? fullscreen) {
		var id = await _mediator.Send<string?>(new GetDefaultMonitorRequest());
		if (string.IsNullOrWhiteSpace(id)) {
			return Redirect("/monitorList");
		}
		var url = $"/monitor/{id}";
		if (fullscreen.HasValue) {
			url += $"?full-screen={fullscreen.Value.ToString().ToLowerInvariant()}";
		}
		return Redirect(url);
	}

	[Route("searchTeams")]
	[HttpGet]
	public IAsyncEnumerable<TeamInfo> SearchTeams([FromQuery]string? searchTerm) => _userManager.GetTeams(searchTerm);
}
