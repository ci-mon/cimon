using System.Collections.Immutable;

namespace Cimon.Contracts;

public record TeamInfo(string Name, ImmutableList<string> ChildTeams);