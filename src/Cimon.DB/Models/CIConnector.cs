using Cimon.Contracts.CI;

namespace Cimon.DB.Models;

public class CIConnector : IEntityCreator<CIConnector>, IComparable<CIConnector>
{
	public static CIConnector Create() => new(){Key = $"Connector{Guid.NewGuid().ToString()[..4]}"};

	public int Id { get; set; }
	public CISystem CISystem { get; set; }
	public string Key { get; set; }

	public List<BuildConfigModel> BuildConfigModels { get; set; } = new();


	public int CompareTo(CIConnector? other) {
		if (ReferenceEquals(this, other)) return 0;
		if (ReferenceEquals(null, other)) return 1;
		return Id.CompareTo(other.Id);
	}
}