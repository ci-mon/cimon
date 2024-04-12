using Akka.Actor;
using Akka.DependencyInjection;
using Cimon.Data.BuildInformation;
using Cimon.Data.Common;

namespace Cimon.Data.Monitors;

public class MonitorServiceActor : ReceiveActor
{
	public MonitorServiceActor(IActorRef buildInfoService) {
		var resolver = Context.System.GetExtension<DependencyResolver>().Resolver;
		resolver.GetService<MonitorService>();
		Receive<ActorsApi.MonitorMessage>(ForwardToMonitor);
		Receive<BuildInfoServiceActorApi.Subscribe>(buildInfoService.Forward);
		Receive<BuildInfoServiceActorApi.Refresh>(buildInfoService.Forward);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(buildInfoService.Forward);
	}

	private void ForwardToMonitor(ActorsApi.MonitorMessage m) {
		var child = Context.GetOrCreateChild<MonitorActor>($"Monitor_{m.Id}", m.Id);
		child.Forward(m);
	}
}
