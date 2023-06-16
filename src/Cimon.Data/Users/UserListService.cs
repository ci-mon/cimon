namespace Cimon.Data.Users;

public class UserListService
{
	public async IAsyncEnumerable<TeamInfo> GetAllTeams() {
		yield return new TeamInfo("rnd");
		yield return new TeamInfo("testers");
	}

	public IAsyncEnumerable<UserInfo> GetUsers(string? searchTerm) {
		return GetAll().Where(x => searchTerm == null || x.Name.Contains(searchTerm));
	}

	public IAsyncEnumerable<TeamInfo> GetTeams(string? searchTerm) {
		return GetAllTeams().Where(x => searchTerm == null || x.Name.Contains(searchTerm));
	}

	private static async IAsyncEnumerable<UserInfo> GetAll() {
		await Task.Yield();
		yield return new UserInfo("test1", new TeamInfo("testers"));
		yield return new UserInfo("test2", new TeamInfo("testers"));
		yield return new UserInfo("bob", new TeamInfo("rnd"));
		yield return new UserInfo("alice", new TeamInfo("rnd"));
	}

}
