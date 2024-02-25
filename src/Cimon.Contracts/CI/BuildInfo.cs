namespace Cimon.Contracts.CI;

public record BuildInfo
{
	public required string Url { get; set; }
	public required string? Group { get; set; }
	public required string? BranchName { get; set; }
	public required string Name { get; set; }
	public required string Id { get; set; }
	public string? StatusText { get; set; }
	public string? Log { get; set; }
	public BuildStatus Status { get; set; }
	public DateTimeOffset? StartDate { get; set; }
	public DateTimeOffset? EndDate => StartDate + Duration;
	public TimeSpan? Duration { get; set; }
	public IReadOnlyCollection<VcsChange> Changes { get; set; } = Array.Empty<VcsChange>();
	public IReadOnlyCollection<CIBuildProblem> Problems { get; set; } = Array.Empty<CIBuildProblem>();
	public IReadOnlyCollection<CITestOccurence> FailedTests { get; set; } = Array.Empty<CITestOccurence>();
	public int CommentsCount { get; set; }
	public BuildFailureSuspect? FailureSuspect { get; set; }
	public static BuildInfo NoData { get; } = new() {
		Url = "NO_DATA",
		Group = "NO_DATA",
		BranchName = "NO_DATA",
		Name = "NO_DATA",
		Id = "NO_DATA",
	};

	public string? Number { get; set; }

	public bool IsNotOk() => Status == BuildStatus.Failed;
	public bool CanHaveDiscussion() => Status is BuildStatus.Failed or BuildStatus.Investigated or BuildStatus.Fixed;
}
