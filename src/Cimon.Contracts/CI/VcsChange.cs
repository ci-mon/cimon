namespace Cimon.Contracts.CI;

using System.Collections.Immutable;

public record VcsChange(VcsUser Author, DateTimeOffset? Date, string CommitMessage, ImmutableArray<FileModification> Modifications);