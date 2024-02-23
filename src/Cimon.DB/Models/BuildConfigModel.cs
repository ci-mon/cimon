using Cimon.Contracts.CI;

namespace Cimon.DB.Models;

public record BuildConfigModel : BuildConfig, IEntityCreator<BuildConfigModel>
{
	public BuildConfigModel() {
	}

	public BuildConfigModel(CIConnector connector, string key, string? name = null, string? branch = null, bool isDefaultBranch = false) 
		:this(){
		Connector = connector;
		Key = key;
		Branch = branch;
		IsDefaultBranch = isDefaultBranch;
		Name = name;
	}
	public int ConnectorId { get; set; }
	public CIConnector Connector { get; set; }
	public BuildConfigStatus Status { get; set; }
	public BuildInfo? DemoState { get; set; }
	public List<BuildInMonitor> Monitors { get; set; } = new();
	public static BuildConfigModel Create() => new();

	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Connector.Id);
	public virtual bool Equals(BuildConfigModel? other) =>
		(other?.Connector.Id.Equals(Connector.Id) ?? false) && base.Equals(other);

}
