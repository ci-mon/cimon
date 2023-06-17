namespace Cimon.Contracts;

public class BuildConfigInfo
{
	public required string Key { get; init; }
	public Dictionary<string, string> Props { get; set; } = new();

	protected bool Equals(BuildConfigInfo other) => Key == other.Key;

	public override int GetHashCode() => Key.GetHashCode();
}
