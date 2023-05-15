using Cimon.Data.Users;

namespace Cimon.Data;

public record BuildInfo
{

	public string BuildHomeUrl { get; set; }

	public string ProjectName { get; set; }

	public string Name { get; set; }

	public string Number { get; set; }

	public string StatusText { get; set; }

	public BuildStatus Status { get; set; }

	public DateTime FinishDate { get; set; }

	public DateTime StartDate { get; set; }

	public string BranchName { get; set; }

	public string Commiters { get; set; }
	public IList<User>? CommitterUsers => Commiters?.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(User.FromFullName).ToList();

	private IList<string> _lastModificationBy;

	public IList<string> LastModificationBy {
		get {
			if (_lastModificationBy == null) {
				_lastModificationBy = new List<string>();
			} else {
				_lastModificationBy = _lastModificationBy
					.Where(x => x.ToLower() != "unknowuser" && x.ToLower() != "bpmonlinebuild").ToList();
			}
			return _lastModificationBy;
		}
	}

	public static BuildInfo CreateErrorBuild(string buildType) {
		return new BuildInfo {
			Name = buildType,
			Number = "0",
			ProjectName = string.Empty,
			Status = BuildStatus.Failed,
			StatusText = string.Format("Build \"{0}\" can't be loaded.", buildType),
			FinishDate = DateTime.Now
		};
	}

	public string GetFinishDateString {
		get { return FinishDate.ToString("dd.MM.yyyy HH:mm:ss"); }
	}

	public string GetStartDateString => StartDate.ToString("HH:mm");

	public string BuildId { get; set; }
	public int CommentsCount { get; set; }

	public bool IsNotOk() => Status == BuildStatus.Failed;

	public bool CanHaveDiscussion() => Status is BuildStatus.Failed or BuildStatus.Investigated or BuildStatus.Fixed;
}