using System.Collections.Immutable;

namespace Cimon.Contracts;

public record UserInfo(string Id, string Name, ImmutableList<TeamInfo> Teams);