namespace Cimon.Data.Users;

class UserNameProvider : IUserNameProvider
{
	private string? _userName;
	public void SetUserName(string name) {
		_userName = name;
	}

	public string? Name => _userName;
}