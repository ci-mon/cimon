namespace Cimon.Contracts.CI;

public record CITestOccurence(string Name)
{

	public string TestId { get; init; }

	public string Details { get; init; }

	public bool? Ignored { get; init; }

	public bool? CurrentlyMuted { get; init; }

	public bool? CurrentlyInvestigated { get; init; }

	public bool? NewFailure { get; init; }

}