namespace Cimon.Contracts;

public readonly record struct UserId(string Id)
{
	public static implicit operator string(UserId id) => id.Id;
	public static implicit operator UserId(string id) => new(id);
	public override string ToString() => Id;
}
