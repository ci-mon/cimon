using Cimon.Data;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Users;

[Route("api/users")]
public class UsersController : Controller
{
	private readonly UserService _userService;

	public UsersController(UserService userService) {
		_userService = userService;
	}

	[Route("search")]
	[HttpGet]
	public IAsyncEnumerable<UserInfo> Search([FromQuery]string? searchTerm) {
		return _userService.GetUsers(searchTerm);
	}

	[Route("searchTeams")]
	[HttpGet]
	public IAsyncEnumerable<TeamInfo> SearchTeams([FromQuery]string? searchTerm) {
		return _userService.GetTeams(searchTerm);
	}
}
