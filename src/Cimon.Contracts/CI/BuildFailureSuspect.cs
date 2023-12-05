namespace Cimon.Contracts.CI;

public record BuildFailureSuspect(VcsUser User, float Confidence)
{
	public static BuildFailureSuspect Empty { get; } = new(new VcsUser(new UserName("", ""), ""), 0);
}
