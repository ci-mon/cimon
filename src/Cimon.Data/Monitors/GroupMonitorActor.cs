using System.Collections.Immutable;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Cimon.Contracts.CI;
using Cimon.Data.Common;
using Cimon.DB.Models;

namespace Cimon.Data.Monitors;

class GroupMonitorActor : ReceiveActor
{
    private readonly IRequiredActor<MonitorServiceActor> _monitorService;
    private readonly Dictionary<int, List<BuildInfoStream>> _connectedMonitorsInfo = new();
    private ImmutableList<ConnectedMonitor> _connectedMonitors = ImmutableList<ConnectedMonitor>.Empty;
    private MonitorModel _model = null!;
    private readonly HashSet<int> _removedMonitors = new ();

    public GroupMonitorActor(IRequiredActor<MonitorServiceActor> monitorService) {
        _monitorService = monitorService;
        Receive<MonitorModel>(HandleGroupMonitorChange);
        Receive<ActorsApi.MonitorInfo>(OnReceiveConnectedMonitorInfo);
    }

    record BuildInfoStream(BuildConfigModel BuildConfig, ReplaySubject<BuildInfo> Subject) :
        IBuildInfoStream, IBuildInfoSnapshot
    {
        public IObservable<BuildInfo> BuildInfo => Subject;
        public BuildInfo? LatestInfo { get; set; }
    }

    private void OnReceiveConnectedMonitorInfo(ActorsApi.MonitorInfo obj) {
        bool isChanged = false;
        bool isBuildChanged = false;
        if (_removedMonitors.Contains(obj.MonitorModel.Id)) return;
        if (!_connectedMonitorsInfo.TryGetValue(obj.MonitorModel.Id, out var value)) {
            value = new List<BuildInfoStream>();
            _connectedMonitorsInfo[obj.MonitorModel.Id] = value;
            isChanged = true;
        }
        var newConfigs = obj.BuildInfos.Select(x => x.BuildConfig.Id).ToHashSet();
        var itemsToRemove = value.Where(x=>!newConfigs.Contains(x.BuildConfig.Id)).ToList();
        foreach (var toRemove in itemsToRemove) {
            value.Remove(toRemove);
        }
        foreach (var buildInfo in obj.BuildInfos) {
            var existing = value.Find(x => x.BuildConfig.Id == buildInfo.BuildConfig.Id);
            if (existing is null) {
                existing = new BuildInfoStream(buildInfo.BuildConfig, new ReplaySubject<BuildInfo>(1));
                value.Add(existing);
                isChanged = true;
            }
            if (buildInfo.LatestInfo is not null) {
                isBuildChanged = true;
                existing.LatestInfo = buildInfo.LatestInfo;
                existing.Subject.OnNext(buildInfo.LatestInfo);
            }
        }
        if (!isChanged && !isBuildChanged) return;
        var builds = GetBuilds();
        if (isChanged || isBuildChanged) {
            Context.Parent.Tell(new ActorsApi.MonitorInfo(_model, builds));
        }
        if (!isChanged) return;
        Context.Parent.Tell(new MonitorData {
            Monitor = _model,
            Builds = builds
        });
    }

    private List<BuildInfoStream> GetBuilds() {
        return _connectedMonitorsInfo.Values.SelectMany(x => x)
            .DistinctBy(x => x.BuildConfig.Id).ToList();
    }

    private void HandleGroupMonitorChange(MonitorModel model) {
        var diff = _connectedMonitors.CompareWith(model.ConnectedMonitors, x => x.ConnectedMonitorModel.Key);
        foreach (var removed in diff.Removed) {
            _connectedMonitorsInfo.Remove(removed.ConnectedMonitorModel.Id);
            _removedMonitors.Add(removed.ConnectedMonitorModel.Id);
            _monitorService.ActorRef.Tell(new ActorsApi.UnWatchMonitorByActor(removed.ConnectedMonitorModel.Key));
        }
        foreach (var connected in diff.Added) {
            _removedMonitors.Remove(connected.ConnectedMonitorModel.Id);
            _monitorService.ActorRef.Tell(new ActorsApi.WatchMonitorByActor(connected.ConnectedMonitorModel.Key));
        }
        _connectedMonitors = model.ConnectedMonitors.ToImmutableList();
        if (diff.Added.Count == 0 && diff.Removed.Count == 0) return;
        Context.Parent.Tell(new MonitorData {
            Monitor = model,
            Builds = ArraySegment<IBuildInfoStream>.Empty
        });
        var builds = GetBuilds();
        Context.Parent.Tell(new ActorsApi.MonitorInfo(_model, builds));
        _model = model;
    }
}