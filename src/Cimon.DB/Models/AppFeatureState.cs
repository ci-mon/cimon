namespace Cimon.DB.Models;

public record AppFeatureState
{
    public int Id { get; init; }
    public required string Code { get; init; }
    public bool Enabled { get; set; }
    public User? User { get; set; }

    public Team? Team { get; set; }
}