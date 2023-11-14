using Akka.Actor;

namespace Cimon.Data.Actors;

class MonitorServiceActor : ReceiveActor
{
	public MonitorServiceActor() {
		Receive<ActorsApi.WatchMonitor>(m => {
			var child = Context.GetOrCreateChild<MonitorActor>(m.Id);
			child.Forward(m);
		});
	}
}