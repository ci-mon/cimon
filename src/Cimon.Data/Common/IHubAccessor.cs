namespace Cimon.Data.Common;

public interface IHubAccessor<out TClient>
{
	public TClient Group(string name);
}
