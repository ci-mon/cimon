namespace Cimon.Data.Users;

public interface IUserNameProvider
{
	void SetUserName(string name);
	string? Name { get; }
}
