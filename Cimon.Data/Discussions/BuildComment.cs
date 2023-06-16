using System.Collections.Immutable;
using Cimon.Contracts;

namespace Cimon.Data.Discussions;

public class BuildComment
{
	public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
	public required string Comment { get; set; }
	public User Author { get; set; } = User.Guest;
	public IImmutableList<UserId> Mentions { get; set; } = ImmutableList<UserId>.Empty;
	public string Id { get; set; } = $"c_{Guid.NewGuid():N}";
	public DateTime? ModifiedOn { get; set; }

	public bool GetCanEditBy(User user) {
		return Author.Id == user.Id || Author.Id == User.Guest.Id;
	}
}
