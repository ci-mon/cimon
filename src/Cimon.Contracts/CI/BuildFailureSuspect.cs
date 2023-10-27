namespace Cimon.Contracts.CI;

public record BuildFailureSuspect(VcsUser User, float Confidence);
