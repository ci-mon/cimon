using Cimon.Contracts;

namespace Cimon.Data.Users;

public interface ICurrentUserAccessor
{
	public Task<User> Current { get; }
}