using Cimon.Contracts;

namespace Cimon.Data.Users;

public class CurrentUserAccessor : ICurrentUserAccessor
{
	private readonly GetCurrentPrincipal _currentPrincipal;
	private readonly UserManager _userManager;
	public CurrentUserAccessor(GetCurrentPrincipal currentPrincipal, UserManager userManager) {
		_currentPrincipal = currentPrincipal;
		_userManager = userManager;
		Current = GetCurrentUser();
	}

	private async Task<User> GetCurrentUser() {
		var principal = await _currentPrincipal();
		return await _userManager.GetUser(principal);
	}

	public Task<User> Current { get; }
	
}