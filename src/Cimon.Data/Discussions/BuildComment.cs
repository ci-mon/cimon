using System.Collections.Immutable;
using Cimon.Contracts;
using Cimon.Contracts.CI;

namespace Cimon.Data.Discussions;

public record BuildComment
{
	public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
	public required string Comment { get; set; }
	public User Author { get; set; } = User.Guest;
	public IImmutableList<MentionedEntityId> Mentions { get; set; } = ImmutableList<MentionedEntityId>.Empty;
	public string Id { get; set; } = $"c_{Guid.NewGuid():N}";
	public DateTime? ModifiedOn { get; set; }
	public BuildInfo BuildInfo { get; set; }
	public bool GetCanEditBy(User user) {
		return Author.Name == user.Name || Author.Name == User.Guest.Name;
	}
}
