using System.Collections.Immutable;
using System.Diagnostics;

namespace Cimon.Data.Discussions;

[DebuggerDisplay("Comments=[Count={Comments.Count}] Status={Status}")]
public record BuildDiscussionState
{
	public IImmutableList<BuildComment> Comments { get; set; } = ImmutableList<BuildComment>.Empty;
	public BuildDiscussionStatus Status { get; set; }
}
