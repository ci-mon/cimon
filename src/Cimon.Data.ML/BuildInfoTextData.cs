namespace Cimon.Data.ML;

using Contracts.CI;

public record BuildInfoTextData(string BuildStatus, IReadOnlyList<(VcsUser, string)> Changes);