using Cimon.Contracts;
using Cimon.Data.Users;
using Microsoft.AspNetCore.Mvc;

namespace Cimon.Users;

[Route("api/users")]
public class UsersController : Controller
{
	private readonly UserManager _userManager;
	public UsersController(UserManager userManager) {
		_userManager = userManager;
	}

	[Route("search")]
	[HttpGet]
	public IAsyncEnumerable<UserInfo> Search([FromQuery]string? searchTerm) => _userManager.GetUsers(searchTerm);

	[Route("searchTeams")]
	[HttpGet]
	public IAsyncEnumerable<TeamInfo> SearchTeams([FromQuery]string? searchTerm) => _userManager.GetTeams(searchTerm);
}
