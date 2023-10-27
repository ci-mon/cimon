namespace Cimon.Data.ML;

using Cimon.Contracts.CI;

public record BuildInfoTextData(string BuildStatus, IReadOnlyList<(VcsUser, string)> Changes);