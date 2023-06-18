namespace Cimon.Contracts;

public record BuildConfigInfo(string Key) 
{
	public Dictionary<string, string> Props { get; set; } = new();
	public virtual bool Equals(BuildConfigInfo? other) => other?.Key.Equals(Key) ?? false;
	public override int GetHashCode() => Key.GetHashCode();
}
