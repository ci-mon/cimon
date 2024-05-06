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
		Receive<ActorsApi.RefreshAllMonitors>(RefreshAllMonitors);
		Receive<BuildInfoServiceActorApi.Subscribe>(buildInfoService.Forward);
		Receive<BuildInfoServiceActorApi.Refresh>(buildInfoService.Forward);
		Receive<BuildInfoServiceActorApi.Unsubscribe>(buildInfoService.Forward);
	}

	private void RefreshAllMonitors(ActorsApi.RefreshAllMonitors obj) {
		var children = Context.GetChildren();
		foreach (var child in children) {
			child.Tell(new ActorsApi.RefreshMonitor(child.Path.Name));
		}
	}

	private void ForwardToMonitor(ActorsApi.MonitorMessage m) {
		var child = Context.GetOrCreateChild<MonitorActor>(m.Id, m.Id);
		child.Forward(m);
	}
}
