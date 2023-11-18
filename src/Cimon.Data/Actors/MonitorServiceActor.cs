using System.Web;
using Akka.Actor;
using Akka.DependencyInjection;
using AngleSharp.Dom;
using Cimon.Data.Monitors;

namespace Cimon.Data.Actors;

class MonitorServiceActor : ReceiveActor
{
	public MonitorServiceActor(IActorRef buildInfoService) {
		var resolver = Context.System.GetExtension<DependencyResolver>().Resolver;
		var monitorService = resolver.GetService<MonitorService>();
		Receive<ActorsApi.WatchMonitor>(m => {
			var child = Context.GetOrCreateChild<MonitorActor>($"Monitor_{m.Id}", out var created);
			if (created)
				child.Tell(monitorService.GetMonitorById(m.Id));
			child.Forward(m);
		});
		Receive<BuildInfoServiceActorApi.Subscribe>(buildInfoService.Forward);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(buildInfoService.Forward);
	}
}
