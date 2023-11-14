using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;

namespace Cimon.Data.Actors;

class MonitorActor : ReceiveActor
{
	private readonly Subject<MonitorData> _subject = new();
	public MonitorActor() {
		var observable = Observable.Create<MonitorData>(observer => {
			var sub = _subject.Subscribe(observer);
			return Disposable.Create(sub, disposable => {
				disposable.Dispose();
				//schedule monitor pause
			});
		});
		Receive<ActorsApi.WatchMonitor>(_ => Sender.Tell(observable));
		// subscribe to monitor changes
		// subscribe for build infos
		// forward infos
	}
}
