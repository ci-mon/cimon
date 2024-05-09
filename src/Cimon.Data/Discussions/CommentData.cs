using System.Collections.Immutable;
using Cimon.Contracts;

namespace Cimon.Data.Discussions;

public class CommentData
{
	public User Author { get; init; } = User.Guest;
	public string Comment { get; set; } = string.Empty;
	public ImmutableList<MentionedEntityId>? Mentions { get; set; }
}
