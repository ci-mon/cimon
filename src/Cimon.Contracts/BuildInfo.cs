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

	public DateTimeOffset FinishDate { get; set; }

	public DateTimeOffset StartDate { get; set; }

	public required string BranchName { get; set; }

	public IReadOnlyCollection<string> Committers { get; set; } = Array.Empty<string>();
	public IReadOnlyCollection<User> CommitterUsers => Committers.Select(FromFullName).ToList();

	private static User FromFullName(string fullName) {
		// TODO move user info extraction to build monitor service 
		var id = fullName.Split(" ").Aggregate((a, b) => $"{Char.ToLowerInvariant(a[0])}.{b.ToLowerInvariant()}");
		return User.Create(id, fullName, new []{new Claim(ClaimTypes.Email, $"{id}@example.com")});
	}

	public required string BuildConfigId { get; set; }
	public int CommentsCount { get; set; }

	public bool IsNotOk() => Status == BuildStatus.Failed;

	public bool CanHaveDiscussion() => Status is BuildStatus.Failed or BuildStatus.Investigated or BuildStatus.Fixed;
}
