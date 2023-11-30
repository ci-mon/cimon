namespace Cimon.DB.Models;

public class CIConnectorSetting : IEntityCreator<CIConnectorSetting>
{
	public static CIConnectorSetting Create() {
		return new CIConnectorSetting();
	}

	public int Id { get; set; }
	public CIConnector CIConnector { get; set; }
	public string Key { get; set; }
	public string Value { get; set; }

}
