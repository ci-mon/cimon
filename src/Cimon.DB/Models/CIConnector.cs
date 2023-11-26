using Cimon.Contracts.CI;

namespace Cimon.DB.Models;

public record CIConnector : IEntityCreator<CIConnector>
{
	public static CIConnector Create() => new(){Key = $"Connector{Guid.NewGuid().ToString()[..4]}"};

	public int Id { get; set; }
	public CISystem CISystem { get; set; }
	public string Key { get; set; }

	public List<BuildConfigModel> BuildConfigModels { get; set; } = new();
	

}

public record CIConnectorSetting : IEntityCreator<CIConnectorSetting>
{
	public static CIConnectorSetting Create() {
		return new CIConnectorSetting();
	}

	public int Id { get; set; }
	public CIConnector CIConnector { get; set; }
	public string Key { get; set; }
	public string Value { get; set; }

}
