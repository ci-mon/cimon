using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Microsoft.Extensions.DependencyInjection;

namespace Cimon.Data.Users;

public class UserActor : ReceiveActor
{
    private ImmutableList<MentionInfo> _mentions = ImmutableList<MentionInfo>.Empty;
    private readonly ReplaySubject<IImmutableList<MentionInfo>> _mentionsSubject = new(1);
    private readonly BuildConfigService _buildConfigService;
    private readonly IServiceScope _scope;
    private readonly IHubAccessor<IUserClientApi> _hubAccessor;
    private readonly IActorRef _monitorActor;
    private IDisposable? _subscription;

    public UserActor(BuildConfigService buildConfigService, IServiceProvider serviceProvider) {
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

    private void SubscribeToMentions(ActorsApi.SubscribeToMentions msg) {
        _mentionSubscriptionCount++;
        if (_subscription is not null) {
            return;
        }
        var userName = Self.Path.Name;
        var mentions = _buildConfigService.GetMentionsWithBuildConfig(_mentionsSubject);
        _subscription = mentions.Select(m => {
            return _hubAccessor.Group(userName).UpdateMentions(m.Select(x =>
                new ExtendedMentionInfo(x.Mention.BuildConfigId, x.Mention.CommentsCount,
                    x.BuildConfig.Map(c => c.Key).ValueOr(string.Empty))));
        })
        .Subscribe();
    }
}
