using System.Collections.Immutable;

namespace Cimon.Contracts.CI;

public record CommitterInfo(VcsUser User, int CommitsCount, float SuspectConfidence);
public record BuildInfo
{
	private const string NoDataValue = "NO_DATA";
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
	public IReadOnlyCollection<CommitterInfo> CombinedCommitters { get; set; } = ArraySegment<CommitterInfo>.Empty;
	public IReadOnlyCollection<CIBuildProblem> Problems { get; set; } = Array.Empty<CIBuildProblem>();
	public IReadOnlyCollection<CITestOccurence> FailedTests { get; set; } = Array.Empty<CITestOccurence>();
	public int CommentsCount { get; set; }
	public static BuildInfo NoData { get; } = new() {
		Url = NoDataValue,
		Group = NoDataValue,
		BranchName = NoDataValue,
		Name = NoDataValue,
		Id = NoDataValue,
	};

	public string? Number { get; set; }

	public bool IsNotOk() => !IsOk();
	public bool IsOk() => Status != BuildStatus.Failed;
	public bool IsSuccess() => Status == BuildStatus.Success;
	public bool CanHaveDiscussion() => Status is BuildStatus.Failed or BuildStatus.Investigated or BuildStatus.Fixed;

	public virtual Uri? GetTestUrl(CITestOccurence testOccurence) {
		return null;
	}
}
