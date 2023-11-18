using Cimon.Contracts.CI;
using Cimon.DB.Models;

namespace Cimon.Data.Actors;

public class MonitorData
{
	public MonitorModel Monitor { get; set; }
	public IEnumerable<IBuildInfoStream> Builds { get; set; }
}

public interface IBuildInfoStream
{
	public BuildConfig BuildConfig { get;}
	public IObservable<BuildInfo> BuildInfo { get; }
}
