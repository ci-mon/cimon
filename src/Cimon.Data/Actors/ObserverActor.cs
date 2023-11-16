using Akka.Actor;

namespace Cimon.Data.Actors;

class ObserverActor<T> : ReceiveActor
{
	private readonly IDisposable _subscription;
	public ObserverActor(IObservable<T> observable) {
		var parent = Context.Parent;
		_subscription = observable.Subscribe(m => parent.Tell(m));
	}
	protected override void PostStop() {
		base.PostStop();
		_subscription?.Dispose();
	}

	public static Props Create(IObservable<T> observable) => Props.Create<ObserverActor<T>>(observable);
}
