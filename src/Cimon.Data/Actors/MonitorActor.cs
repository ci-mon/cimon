using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.Monitors;
using Cimon.DB.Models;

namespace Cimon.Data.Actors;

class MonitorActor : ReceiveActor, IWithUnboundedStash
{
	private readonly Subject<MonitorData> _subject = new();
	private MonitorModel _model;
	private readonly IObservable<MonitorData> _dataObservable;

	public MonitorActor(MonitorService service) {
		IObservable<MonitorModel> monitorSubject = service.GetMonitorById(Self.Path.Name);
		Context.Observe(monitorSubject);
		_dataObservable = Observable.Create<MonitorData>(observer => {
			var sub = _subject.Subscribe(observer);
			return Disposable.Create(sub, disposable => {
				disposable.Dispose();
				//schedule monitor pause
			});
		});
		Receive<MonitorModel>(model => {
			OnMonitorChange(model);
			Become(Ready);
		});
		ReceiveAny(_ => Stash.Stash());
		// subscribe to monitor changes
		// subscribe for build infos
		// forward infos
	}

	private void OnMonitorChange(MonitorModel model) {
		_model = model;
	}

	private void Ready() {
		Receive<MonitorModel>(OnMonitorChange);
		Receive<ActorsApi.WatchMonitor>(_ => Sender.Tell(_dataObservable));
		Stash.UnstashAll();
	}

	public IStash Stash { get; set; }
}
