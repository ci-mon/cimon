using Cimon.Contracts.CI;
using Cimon.DB.Models;

namespace Cimon.Data.Monitors;

public class MonitorData
{
	public MonitorModel Monitor { get; set; }
	public IEnumerable<IBuildInfoStream> Builds { get; set; }
}

public interface IBuildInfoStream
{
	public BuildConfigModel BuildConfig { get;}
	public IObservable<BuildInfo> BuildInfo { get; }
}

public interface IBuildInfoSnapshot
{
	public BuildConfigModel BuildConfig { get;}
	public BuildInfo? LatestInfo { get; }

}
