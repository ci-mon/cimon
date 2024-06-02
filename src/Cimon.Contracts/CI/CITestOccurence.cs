namespace Cimon.Contracts.CI;

public record CITestOccurence(string Name)
{
	public required string TestId { get; init; }

	public required string Details { get; init; }

	public bool? Ignored { get; init; }

	public bool? CurrentlyMuted { get; init; }

	public bool? CurrentlyInvestigated { get; init; }

	public bool? NewFailure { get; init; }
	public string Summary => string.IsNullOrWhiteSpace(Name) ? TestId : Name;
}
