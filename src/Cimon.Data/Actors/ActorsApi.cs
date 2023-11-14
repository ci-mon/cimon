using System.Reactive.Subjects;

namespace Cimon.Data.Actors;

public class ActorsApi
{
	public record WatchMonitor(string Id) : IMessageWithResponse<IObservable<MonitorData>>;
}
