using System.Collections.Immutable;
using System.Security.Claims;

namespace Cimon.Contracts;

public enum FileModificationType
{
	Unknown,
	Add,
	Delete,
	Edit,
	Move,
	Copy
}

public record struct FileModification(FileModificationType Type, string Path);

public record VCSChange(UserName Author, DateTimeOffset Date, string CommitMessage, ImmutableArray<FileModification> Modifications);

public record BuildInfo
{

	public required string Url { get; set; }

	public required string? Group { get; set; }
	public required string? BranchName { get; set; }

	public required string Name { get; set; }

	public required string Number { get; set; }

	public string? StatusText { get; set; }
	public string? Log { get; set; }

	public BuildStatus Status { get; set; }

	public DateTimeOffset StartDate { get; set; }
	public DateTimeOffset EndDate => StartDate + Duration; 

	public TimeSpan Duration { get; set; }

	public IReadOnlyCollection<string> Committers { get; set; } = Array.Empty<string>();
	public IReadOnlyCollection<VCSChange> Changes { get; set; } = Array.Empty<VCSChange>();
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
