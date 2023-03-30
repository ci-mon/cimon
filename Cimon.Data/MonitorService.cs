namespace Cimon.Data;

using System.Reactive.Linq;
using System.Reactive.Subjects;

public class Monitor
{
	public string Id { get; set; }

	public List<BuildLocator> Builds { get; set; } = new();

}

public class MonitorService
{
	public MonitorService() {
		_monitorsSubject = new(_monitors);
	}

	private BehaviorSubject<IList<Monitor>> _monitorsSubject { get; }

	private int counter = 0;
	private List<Monitor> _monitors = new();

	public IObservable<IList<Monitor>> GetMonitors() {
		_monitors = new List<Monitor> {
			new() {
				Id = $"default{++counter}",
			},
			new Monitor {
				Id = "bpms",
				Builds = {
					new BuildLocator {
						CiSystem = CISystem.TeamCity,
						Id = "BpmsPlatformWorkDiagnostic"
					},
					new BuildLocator {
						CiSystem = CISystem.TeamCity,
						Id = "BpmsPlatformWorkDiagnostic2"
					}
				}
			},
		};
		_monitorsSubject.OnNext(_monitors);
		return _monitorsSubject;
	}

	public void Add() {
		_monitors.Add(new Monitor {
			Id = $"bpms{++counter}",
			Builds = new List<BuildLocator> {
				new BuildLocator() {
					Id = "xxx",
					CiSystem = CISystem.TeamCity
				}
			}
		});
		_monitorsSubject.OnNext(_monitors);
	}

	public IObservable<Monitor?> GetMonitorById(string monitorId) {
		return GetMonitors().Select(m => m.FirstOrDefault(x => x.Id == monitorId));
	}
}
