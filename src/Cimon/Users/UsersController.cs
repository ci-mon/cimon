using System.Reactive.Linq;
using Cimon.Contracts;
using Cimon.Data.Monitors;
using Cimon.Data.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Users;

[Route("api/users")]
public class UsersController : Controller
{
	private readonly UserManager _userManager;
	private readonly IMediator _mediator;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	private readonly MonitorService _monitorService;
	private readonly ILogger _logger;
	public UsersController(UserManager userManager, IMediator mediator, ICurrentUserAccessor currentUserAccessor, 
			MonitorService monitorService, ILogger<UsersController> logger) {
		_userManager = userManager;
		_mediator = mediator;
		_currentUserAccessor = currentUserAccessor;
		_monitorService = monitorService;
		_logger = logger;
	}

	[Route("search")]
	[HttpGet]
	public IAsyncEnumerable<UserInfo> Search([FromQuery]string? searchTerm) => _userManager.GetUsers(searchTerm);

	[Route("openLastMonitor")]
	[HttpGet]
	public async Task<IActionResult> OpenLastMonitor([FromQuery(Name = "full-screen")]bool? fullscreen) {
		var user = await _currentUserAccessor.Current;
		var id = await _mediator.Send<string?>(new GetDefaultMonitorRequest(user));
		if (!string.IsNullOrWhiteSpace(id)) {
			try {
				var monitor = await _monitorService.GetMonitorById(id)
					.Timeout(TimeSpan.FromSeconds(5))
					.FirstOrDefaultAsync();
				if (monitor != null) {
					var url = $"/monitor/{id}";
					if (fullscreen.HasValue) {
						url += $"?full-screen={fullscreen.Value.ToString().ToLowerInvariant()}";
					}
					return Redirect(url);
				}
			} catch (Exception e) {
				_logger.LogError(e, "Failed to get last monitor for user {User}", user.Name);
				await _mediator.Publish(new MonitorOpenedNotification(user, null));
			}
		}
		return Redirect("/monitorList");
	}

	[Route("searchTeams")]
	[HttpGet]
	public IAsyncEnumerable<TeamInfo> SearchTeams([FromQuery]string? searchTerm) => _userManager.GetTeams(searchTerm);
}
