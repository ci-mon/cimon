namespace Cimon.Contracts;

public record BuildLocator
{
	public CISystem CiSystem { get; set; }

	public required string Id { get; set; }
	
}
