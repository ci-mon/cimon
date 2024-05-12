namespace Cimon.Contracts.CI;

public record CIBuildProblem(CIBuildProblemType Type, string ShortSummary, string Details, bool? NewFailure)
{
	public string Summary => string.IsNullOrWhiteSpace(Details) ? ShortSummary : Details;
}
