using System.Web;
using Akka.Actor;
using Akka.DependencyInjection;

namespace Cimon.Data.Common;

public static class ActorUtils
{
	public static DependencyResolver DI(this ActorSystem actorSystem) => DependencyResolver.For(actorSystem);
	public static IActorRef DIActorOf<TActor>(this IUntypedActorContext context, string? name = null)
			where TActor : ActorBase =>
		context.ActorOf(DependencyResolver.For(context.System).Props<TActor>(), GetActorName(name));
	public static void Observe<TItem>(this IUntypedActorContext context, IObservable<TItem> subject, 
			string? name = null) {
		context.ActorOf(ObserverActor<TItem>.Create(subject));
	}

	public static IActorRef GetOrCreateChild<TActor>(this IUntypedActorContext context, string? name = null) 
			where TActor : ActorBase {
		return context.GetOrCreateChild<TActor>(name, out _);
	}

	public static IActorRef GetOrCreateChild<TActor>(this IUntypedActorContext context, string? name, out bool created)
			where TActor : ActorBase {
		name = GetActorName(name);
		var child = context.Child(name);
		created = false;
		if (child.IsNobody()) {
			created = true;
			child = context.DIActorOf<TActor>(name);
		}
		return child;
	}

	private static string? GetActorName(string? name) {
		if (!ActorPath.IsValidPathElement(name)) {
			name = HttpUtility.UrlEncode(name);
		}
		return name;
	}

	public static Task<T> Ask<T>(this ICanTell actor, IMessageWithResponse<T> message, CancellationToken? token  = null)
		=> actor.Ask<T>(message, cancellationToken: token ?? CancellationToken.None);
}
