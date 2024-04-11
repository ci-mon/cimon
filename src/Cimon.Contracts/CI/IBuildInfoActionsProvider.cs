namespace Cimon.Contracts.CI;

public class BuildInfoActionDescriptor
{
	public Guid Id { get; init; }
	public string? GroupDescription { get; init; }
	public required string Description { get; init; }
	public required Func<Task> Execute { get; set; }
}

public interface IBuildInfoActionsProvider
{
	public IReadOnlyCollection<BuildInfoActionDescriptor> GetAvailableActions();
}