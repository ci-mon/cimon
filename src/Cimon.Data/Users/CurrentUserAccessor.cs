using System.Security.Claims;
using Cimon.Contracts;

namespace Cimon.Data.Users;

public class CurrentUserAccessor : ICurrentUserAccessor
{
	private readonly GetCurrentPrincipal _currentPrincipal;
	private readonly UserManager _userManager;
	private readonly IUserNameProvider _userNameProvider;

	public CurrentUserAccessor(GetCurrentPrincipal currentPrincipal, UserManager userManager,
			IUserNameProvider userNameProvider) {
		_currentPrincipal = currentPrincipal;
		_userManager = userManager;
		_userNameProvider = userNameProvider;
		Current = GetCurrentUser();
	}

	private async Task<User> GetCurrentUser() {
		if (_userNameProvider.Name is { } name) {
			return await _userManager.GetUser(name);
		}
		var principal = await _currentPrincipal();
		return await _userManager.GetUser(principal);
	}

	public Task<User> Current { get; }
	
}
