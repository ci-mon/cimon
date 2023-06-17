namespace Cimon.Contracts;

public record BuildInfo
{

	public required string BuildHomeUrl { get; set; }

	public required string ProjectName { get; set; }

	public required string Name { get; set; }

	public required string Number { get; set; }

	public string? StatusText { get; set; }

	public BuildStatus Status { get; set; }

	public DateTime FinishDate { get; set; }

	public DateTime StartDate { get; set; }

	public required string BranchName { get; set; }

	public required string Committers { get; set; }

	public IReadOnlyCollection<User> CommitterUsers => Committers?.Split(",", StringSplitOptions.RemoveEmptyEntries)
		.Select(User.FromFullName).ToList() ?? (IReadOnlyCollection<User>)ArraySegment<User>.Empty;

	private IReadOnlyCollection<string>? _lastModificationBy;

	public IReadOnlyCollection<string> LastModificationBy {
		get {
			if (_lastModificationBy == null) {
				_lastModificationBy = new List<string>();
			} else {
				_lastModificationBy = _lastModificationBy
					.Where(x => x.ToLower() != "unknownuser" && x.ToLower() != "bpmonlinebuild").ToList();
			}
			return _lastModificationBy;
		}
	}

	public string GetFinishDateString => FinishDate.ToString("dd.MM.yyyy HH:mm:ss");

	public string GetStartDateString => StartDate.ToString("HH:mm");

	public required string BuildConfigId { get; set; }
	public int CommentsCount { get; set; }

	public bool IsNotOk() => Status == BuildStatus.Failed;

	public bool CanHaveDiscussion() => Status is BuildStatus.Failed or BuildStatus.Investigated or BuildStatus.Fixed;
}
