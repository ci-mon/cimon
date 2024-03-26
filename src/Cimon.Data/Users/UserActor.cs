using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Contracts.CI;
using Cimon.Data.BuildInformation;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Users;

class Debouncer<TIn, TOut>: ReceiveActor where TIn : IEquatable<TIn>
{
    public record DelayedMessage(TIn Message);

    private ICancelable? _scheduled;
    public Debouncer(TimeSpan debounceTime, Func<TIn, TOut> converter) {
        Receive<TIn>(msg => {
            _scheduled?.Cancel();
            _scheduled =
                Context.System.Scheduler.ScheduleTellOnceCancelable(debounceTime, Self,
                    new DelayedMessage(msg), Sender);
        });
        Receive<DelayedMessage>(msg => {
            var outMsg = converter(msg.Message);
            Context.Parent.Tell(outMsg);
        });
    }

}

public class UserActor : ReceiveActor
{
    
    private ImmutableList<MentionInfo> _mentions = ImmutableList<MentionInfo>.Empty;
    private readonly ReplaySubject<IImmutableList<MentionInfo>> _mentionsSubject = new(1);
    private readonly BuildConfigService _buildConfigService;
    private readonly IServiceScope _scope;
    private readonly IHubAccessor<IUserClientApi> _hubAccessor;
    private readonly IActorRef _monitorActor;
    private IDisposable? _subscription;

    public UserActor(BuildConfigService buildConfigService, IServiceProvider serviceProvider, 
            BuildInfoMonitoringSettings monitoringSettings) {
        _buildConfigService = buildConfigService;
        _scope = serviceProvider.CreateScope();
        _hubAccessor = _scope.ServiceProvider.GetRequiredKeyedService<IHubAccessor<IUserClientApi>>("UserHub");
        var nameProvider = _scope.ServiceProvider.GetRequiredService<IUserNameProvider>();
        nameProvider.SetUserName(Self.Path.Name);
        _monitorActor = AppActors.Instance.MonitorService;
        Receive<ActorsApi.UserMessage<MentionInfo>>(OnMention);
        Receive<ActorsApi.GetUserMentions>(_ => Sender.Tell(_mentionsSubject));
        Receive<ActorsApi.SubscribeToMentions>(SubscribeToMentions);
        Receive<ActorsApi.UnSubscribeOnMentions>(UnSubscribeOnMentions);
        Receive<CheckMentionSubscriptionsCount>(OnCheckMentionSubscriptionsCount);
        Receive<ActorsApi.SubscribeToMonitor>(SubscribeToMonitor);
        Receive<ActorsApi.UnSubscribeFromMonitor>(UnSubscribeFromMonitor);
        Receive<ActorsApi.UpdateLastMonitor>(UpdateLastMonitor);
        var delay = TimeSpan.FromMilliseconds(500);
        var monitorInfoDebounce =
            Context.ActorOf(Props.Create<Debouncer<ActorsApi.MonitorInfo, MonitorInfo>>(delay, Convert));
        Receive<ActorsApi.MonitorInfo>(m => monitorInfoDebounce.Forward(m));
        ReceiveAsync<MonitorInfo>(OnMonitorInfoUpdated);
    }

    private MonitorInfo Convert(ActorsApi.MonitorInfo monitorInfo) {
        var failedBuildsCount = monitorInfo.BuildInfos.Count(x => x.LatestInfo?.Status == BuildStatus.Failed);
        return new MonitorInfo {
            MonitorKey = monitorInfo.MonitorModel.Key,
            FailedBuildsCount = failedBuildsCount
        };
    }

    private async Task OnMonitorInfoUpdated(MonitorInfo msg) {
        if (_latestMonitorInfo?.Equals(msg) == true) return;
        _latestMonitorInfo = msg;
        var userName = GetUserName();
        await _hubAccessor.Group(userName).UpdateMonitorInfo(msg);
    }

    private void OnMention(ActorsApi.UserMessage<MentionInfo> msg) {
        MentionInfo mention = msg.Payload;
        var delta = mention.CommentsCount;
        var mentionInfo = _mentions.Find(x => x.BuildConfigId == mention.BuildConfigId);
        if (mentionInfo is not null) {
            var newValue = mentionInfo with { CommentsCount = mentionInfo.CommentsCount + delta };
            _mentions = newValue.CommentsCount == 0
                ? _mentions.Remove(mentionInfo)
                : _mentions.Replace(mentionInfo, newValue);
        } else {
            _mentions = _mentions.Add(mention);
        }
        _mentionsSubject.OnNext(_mentions);
    }

    private void SubscribeToMonitor(ActorsApi.SubscribeToMonitor msg) {
        _latestMonitorInfo = null;
        _watchLastMonitor = true;
        _lastMonitorId = msg.MonitorId;
        if (string.IsNullOrEmpty(msg.MonitorId)) {
            return;
        }
        _monitorActor.Tell(new ActorsApi.WatchMonitorByActor(msg.MonitorId));
    }

    private void UpdateLastMonitor(ActorsApi.UpdateLastMonitor msg) {
        if (msg.MonitorId == _lastMonitorId) return;
        if (!_watchLastMonitor) return;
        if (!string.IsNullOrEmpty(_lastMonitorId)) {
            _monitorActor.Tell(new ActorsApi.UnWatchMonitorByActor(_lastMonitorId!));
        }
        _lastMonitorId = msg.MonitorId;
        if (!string.IsNullOrEmpty(_lastMonitorId)) {
            _monitorActor.Tell(new ActorsApi.WatchMonitorByActor(_lastMonitorId));
        }
    }

    private void UnSubscribeFromMonitor(ActorsApi.UnSubscribeFromMonitor msg) {
        var monitorId = msg.MonitorId ?? _lastMonitorId;
        if (monitorId is null) return;
        _monitorActor.Tell(new ActorsApi.UnWatchMonitorByActor(monitorId));
        _lastMonitorId = null;
    }

    private void OnCheckMentionSubscriptionsCount(CheckMentionSubscriptionsCount obj) {
        if (_mentionSubscriptionCount != 0) return;
        _subscription?.Dispose();
        _subscription = null;
    }

    protected override void PostStop() {
        base.PostStop();
        _subscription?.Dispose();
        _scope.Dispose();
    }

    record CheckMentionSubscriptionsCount;
    private void UnSubscribeOnMentions(ActorsApi.UnSubscribeOnMentions arg) {
        _mentionSubscriptionCount--;
        if (_mentionSubscriptionCount == 0) {
            Context.System.Scheduler.ScheduleTellOnce(TimeSpan.FromSeconds(30), Self,
                new CheckMentionSubscriptionsCount(), Self);
        }
    }

    private int _mentionSubscriptionCount;
    private string? _lastMonitorId;
    private bool _watchLastMonitor;
    private MonitorInfo? _latestMonitorInfo;
    private readonly BehaviorSubject<IReadOnlyCollection<ExtendedMentionInfo>> _mentionsWithBuildConfig =
        new(new List<ExtendedMentionInfo>());
    private void SubscribeToMentions(ActorsApi.SubscribeToMentions msg) {
        _mentionSubscriptionCount++;
        if (_subscription is not null) {
            msg.Caller.UpdateMentions(_mentionsWithBuildConfig.Value);
            return;
        }
        var userName = GetUserName();
        var mentions = _buildConfigService.GetMentionsWithBuildConfig(_mentionsSubject);
        var multicast = mentions.Select(m => m.Select(x => new ExtendedMentionInfo(x.Mention.BuildConfigId,
                x.Mention.CommentsCount, x.BuildConfig.Map(c => c.Key).ValueOr(string.Empty))).ToList())
            .Multicast(_mentionsWithBuildConfig);
        _subscription = multicast.Subscribe(infos => {
                var clients = _hubAccessor.Group(userName);
                clients.UpdateMentions(infos);
            });
        multicast.Connect();
    }

    private string GetUserName() => Self.Path.Name;
}
