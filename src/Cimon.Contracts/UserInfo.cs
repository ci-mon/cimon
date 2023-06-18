using System.Collections.Immutable;

namespace Cimon.Contracts;

public record UserInfo(string Id, string Name, string? Team, ImmutableList<TeamInfo> Teams);