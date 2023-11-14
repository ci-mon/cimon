using Akka.Actor;
using Akka.DependencyInjection;

namespace Cimon.Data.Actors;

public static class ActorUtils
{
	public static DependencyResolver DI(this ActorSystem actorSystem) => DependencyResolver.For(actorSystem);
	public static IActorRef DIActorOf<TActor>(this IUntypedActorContext context, string? name = null)
			where TActor : ActorBase =>
		context.ActorOf(DependencyResolver.For(context.System).Props<TActor>(), name);

	public static IActorRef GetOrCreateChild<TActor>(this IUntypedActorContext context, string? name = null)
			where TActor : ActorBase {
		var child = context.Child(name);
		if (child.IsNobody()) {
			child = context.DIActorOf<TActor>(name);
		}
		return child;
	}

	public static Task<T> Ask<T>(this ICanTell actor, IMessageWithResponse<T> message, CancellationToken? token  = null)
		=> actor.Ask<T>(message, cancellationToken: token ?? CancellationToken.None);
}
