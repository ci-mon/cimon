namespace Cimon.Contracts.CI;

public record CIBuildProblem(CIBuildProblemType Type, string ShortSummary, string Details, bool? NewFailure);