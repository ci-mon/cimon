using System.Reactive.Subjects;
using Cimon.Contracts.CI;
using Cimon.DB.Models;

namespace Cimon.Data.Actors;

public class MonitorData
{
	public MonitorModel Monitor { get; set; }
	public ISubject<IList<BuildInfo>> Builds { get; set; }
}
