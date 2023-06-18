using Cimon.Contracts;

namespace Cimon.Data.Tests;

[TestFixture]
public class ContractsTests
{
	[Test]
	public void UserEquals() {
		var user1 = User.Create("name1", "Some user");
		var user2 = User.Create("name1", "Some user");
		var user3 = User.Create("name2", "Some user");
		(user1 == user3).Should().BeFalse();
	}
}