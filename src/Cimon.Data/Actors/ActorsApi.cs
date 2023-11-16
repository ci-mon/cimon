namespace Cimon.Data.Actors;

public class ActorsApi
{
	public record WatchMonitor(string Id) : IMessageWithResponse<IObservable<MonitorData>>;
}
