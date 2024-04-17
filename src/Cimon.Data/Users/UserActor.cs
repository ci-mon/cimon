using System.Collections.Immutable;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Akka.Actor;
using Akka.Hosting;
using Cimon.Contracts;
using Cimon.Contracts.CI;
using Cimon.Data.CIConnectors;
using Cimon.Data.Common;
using Cimon.Data.Discussions;
using Cimon.Data.Monitors;
using MediatR;
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

public class UserActor : ReceiveActor, IWithUnboundedStash
{
    private ImmutableList<MentionInfo> _mentions = ImmutableList<MentionInfo>.Empty;
    private readonly ReplaySubject<IImmutableList<MentionInfo>> _mentionsSubject = new(1);
    private readonly BuildConfigService _buildConfigService;
    private readonly IMediator _mediator;
    private readonly IServiceScope _scope;
    private readonly IHubAccessor<IUserClientApi> _hubAccessor;
    private readonly IActorRef _monitorServiceActor;
    private IDisposable? _subscription;
    private User _user = null!;
    private int _mentionSubscriptionCount;
    private string? _lastMonitorId;
    private MonitorInfo? _latestMonitorInfo;
    private readonly BehaviorSubject<IReadOnlyCollection<ExtendedMentionInfo>> _mentionsWithBuildConfig =
        new(new List<ExtendedMentionInfo>());

    public UserActor(UserManager userManager, BuildConfigService buildConfigService, IServiceProvider serviceProvider, 
            IRequiredActor<MonitorServiceActor> monitorService) {
        _buildConfigService = buildConfigService;
        _scope = serviceProvider.CreateScope();
        _hubAccessor = _scope.ServiceProvider.GetRequiredKeyedService<IHubAccessor<IUserClientApi>>("UserHub");
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        var nameProvider = _scope.ServiceProvider.GetRequiredService<IUserNameProvider>();
        nameProvider.SetUserName(Self.Path.Name);
        _monitorServiceActor = monitorService.ActorRef;
        Receive<ActorsApi.UserMessage<MentionInfo>>(OnMention);
        Receive<ActorsApi.GetUserMentions>(_ => Sender.Tell(_mentionsSubject));
        Receive<ActorsApi.SubscribeToMentions>(SubscribeToMentions);
        Receive<ActorsApi.UnSubscribeOnMentions>(UnSubscribeOnMentions);
        Receive<CheckMentionSubscriptionsCount>(OnCheckMentionSubscriptionsCount);
        ReceiveAsync<ActorsApi.SubscribeToLastMonitor>(SubscribeToMonitor);
        Receive<ActorsApi.UnSubscribeFromLastMonitor>(UnSubscribeFromMonitor);
        Receive<ActorsApi.UpdateLastMonitor>(UpdateLastMonitor);
        var delay = TimeSpan.FromMilliseconds(500);
        var monitorInfoDebounce =
            Context.ActorOf(Props.Create<Debouncer<ActorsApi.MonitorInfo, MonitorInfo>>(delay, Convert));
        Receive<ActorsApi.MonitorInfo>(m => monitorInfoDebounce.Forward(m));
        ReceiveAsync<MonitorInfo>(OnMonitorInfoUpdated);
        userManager.GetUser(Self.Path.Name).PipeTo(Self);
        BecomeStacked(() => {
            Receive<User>(user => {
                _user = user;
                UnbecomeStacked();
                Stash.UnstashAll();
            });
            ReceiveAny(_ => Stash.Stash());
        });
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
        await SendMonitorInfo(msg);
    }

    private async Task SendMonitorInfo(MonitorInfo msg) {
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

    private async Task SubscribeToMonitor(ActorsApi.SubscribeToLastMonitor msg) {
        var monitorId = await _mediator.Send<string?>(new GetDefaultMonitorRequest(_user));
        if (!ChangeWatchedMonitor(monitorId) && _latestMonitorInfo is not null) {
            await SendMonitorInfo(_latestMonitorInfo);
        }
    }

    private void UpdateLastMonitor(ActorsApi.UpdateLastMonitor msg) => ChangeWatchedMonitor(msg.MonitorId);
    private void UnSubscribeFromMonitor(ActorsApi.UnSubscribeFromLastMonitor msg) => ChangeWatchedMonitor(null);

    private bool ChangeWatchedMonitor(string? monitorId) {
        if (monitorId == _lastMonitorId) return false;
        _latestMonitorInfo = null;
        if (!string.IsNullOrEmpty(_lastMonitorId)) {
            _monitorServiceActor.Tell(new ActorsApi.UnWatchMonitorByActor(_lastMonitorId!));
        }
        _lastMonitorId = monitorId;
        if (!string.IsNullOrEmpty(_lastMonitorId)) {
            _monitorServiceActor.Tell(new ActorsApi.WatchMonitorByActor(_lastMonitorId));
        }
        return true;
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
    public IStash Stash { get; set; } = null!;
}
