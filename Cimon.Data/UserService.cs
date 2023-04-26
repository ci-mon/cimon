﻿namespace Cimon.Data;

public record UserInfo(string Name, TeamInfo Team);

public record TeamInfo(string Name);
public class UserService
{
	public async IAsyncEnumerable<TeamInfo> GetAllTeams() {
		yield return new TeamInfo("rnd");
		yield return new TeamInfo("testers");
	}

	public IAsyncEnumerable<UserInfo> GetUsers(string? searchTerm) {
		return GetAll().Where(x => searchTerm == null || x.Name.Contains(searchTerm));
	}

	private static async IAsyncEnumerable<UserInfo> GetAll() {
		await Task.Yield();
		yield return new UserInfo("test", new TeamInfo("testers"));
		yield return new UserInfo("bob", new TeamInfo("rnd"));
		yield return new UserInfo("alice", new TeamInfo("rnd"));
	}
	
	public string GetEmail(string userName) => $"{userName}@creatio.com";
}
