namespace Cimon.Data.Users;

public class CurrentUserAccessor : ICurrentUserAccessor
{
	private readonly GetCurrentPrincipal _currentPrincipal;
	public CurrentUserAccessor(GetCurrentPrincipal currentPrincipal) {
		_currentPrincipal = currentPrincipal;
		Current = GetCurrentUser();
	}

	private async Task<User> GetCurrentUser() {
		var principal = await _currentPrincipal();
		var name = principal?.Identity?.Name;
		return string.IsNullOrWhiteSpace(name) ? User.Guest : new User(name, $"U:{name}");
	}

	public Task<User> Current { get; }
	
}