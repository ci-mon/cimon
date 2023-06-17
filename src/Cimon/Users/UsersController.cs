using Cimon.Data.Users;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Users;

[Route("api/users")]
public class UsersController : Controller
{
	private readonly UserListService _userListService;

	public UsersController(UserListService userListService) {
		_userListService = userListService;
	}

	[Route("search")]
	[HttpGet]
	public IAsyncEnumerable<UserInfo> Search([FromQuery]string? searchTerm) {
		return _userListService.GetUsers(searchTerm);
	}

	[Route("searchTeams")]
	[HttpGet]
	public IAsyncEnumerable<TeamInfo> SearchTeams([FromQuery]string? searchTerm) {
		return _userListService.GetTeams(searchTerm);
	}
}
