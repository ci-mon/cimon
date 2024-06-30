using System.Collections.Immutable;
using Cimon.Contracts;
using Cimon.Contracts.CI;
using Cimon.Data.BuildInformation;

namespace Cimon.Data.Discussions;

public class CommentData
{
	public User Author { get; init; } = User.Guest;
	public string Comment { get; set; } = string.Empty;
	public ImmutableList<MentionedEntityId>? Mentions { get; set; }
	public BuildInfo BuildInfo { get; set; }
	public BuildInfoHistory.BuildConfigurationStats? BuildStats { get; set; }
}
