using Cimon.Users;
using FluentAssertions;

namespace Cimon.Tests.Users;

[TestFixture]
[TestOf(typeof(UserHub))]
public class UserHubTest
{

	[Test]
	public void Name_ShouldBeUserHub() {
		nameof(UserHub).Should().Be("UserHub", "because it is used as key in DI container");
	}
}
