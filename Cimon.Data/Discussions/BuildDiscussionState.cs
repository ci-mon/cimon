using System.Collections.Immutable;

namespace Cimon.Data;

public record BuildDiscussionState
{
	public IImmutableList<BuildComment> Comments { get; set; } = ImmutableList<BuildComment>.Empty;
	public BuildDiscussionStatus Status { get; set; }
}