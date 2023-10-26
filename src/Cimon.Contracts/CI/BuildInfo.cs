namespace Cimon.Contracts.CI;

public record BuildInfo
{
	required public string Url { get; set; }
	required public string? Group { get; set; }
	required public string? BranchName { get; set; }
	required public string Name { get; set; }
	required public string Number { get; set; }
	public string? StatusText { get; set; }
	public string? Log { get; set; }
	public BuildStatus Status { get; set; }
	public DateTimeOffset? StartDate { get; set; }
	public DateTimeOffset? EndDate => StartDate + Duration; 
	public TimeSpan? Duration { get; set; }
	public IReadOnlyCollection<VcsChange> Changes { get; set; } = Array.Empty<VcsChange>();
	public IReadOnlyCollection<CIBuildProblem> Problems { get; set; } = Array.Empty<CIBuildProblem>();
	public IReadOnlyCollection<CITestOccurence> FailedTests { get; set; } = Array.Empty<CITestOccurence>();
	required public string BuildConfigId { get; set; }
	public int CommentsCount { get; set; }
	public bool IsNotOk() => Status == BuildStatus.Failed;
	public bool CanHaveDiscussion() => Status is BuildStatus.Failed or BuildStatus.Investigated or BuildStatus.Fixed;
}
