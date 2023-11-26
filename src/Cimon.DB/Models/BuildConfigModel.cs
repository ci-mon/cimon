using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Cimon.Contracts.CI;

namespace Cimon.DB.Models;

public record BuildConfigModel : BuildConfig
{
	public BuildConfigModel() {
	}

	public BuildConfigModel(CIConnector connector, string key, string? branch = null, bool isDefaultBranch = false) 
		:this(){
		Connector = connector;
		Key = key;
		Branch = branch;
		IsDefaultBranch = isDefaultBranch;
	}
	public CIConnector Connector { get; set; }
	public BuildConfigStatus Status { get; set; }
	public BuildInfo? DemoState { get; set; }
	public List<BuildInMonitor> Monitors { get; set; } = new();
	public override int GetHashCode() => HashCode.Combine(base.GetHashCode(), Connector.Id);
	public virtual bool Equals(BuildConfigModel? other) =>
		(other?.Connector.Id.Equals(Connector.Id) ?? false) && base.Equals(other);
	public Dictionary<string, string> Props { get; set; }

}
