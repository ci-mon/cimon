using System.Reactive.Subjects;
using Cimon.Contracts.CI;
using Monitor = Cimon.DB.Models.Monitor;

namespace Cimon.Data.Actors;

public class MonitorData
{
	public Monitor Monitor { get; set; }
	public ISubject<IList<BuildInfo>> Builds { get; set; }
}