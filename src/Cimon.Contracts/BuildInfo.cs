using System.Collections.Immutable;
using System.Security.Claims;

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
		.Select(FromFullName).ToList() ?? (IReadOnlyCollection<User>)ArraySegment<User>.Empty;

	private static User FromFullName(string fullName) {
		// TODO move user info extraction to build monitor service 
		var id = fullName.Split(" ").Aggregate((a, b) => $"{Char.ToLowerInvariant(a[0])}.{b.ToLowerInvariant()}");
		return User.Create(id, fullName, new []{new Claim(ClaimTypes.Email, $"{id}@example.com")});
	}

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
