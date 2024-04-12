using Akka.Actor;
using Akka.Hosting;
using Cimon.Contracts;
using MediatR;

namespace Cimon.Data.Users;


public record MonitorOpenedNotification(User User, string? MonitorId) : INotification;

public record GetDefaultMonitorRequest(User User) : IRequest<string?>, IRequest;


class MonitorHandler : INotificationHandler<MonitorOpenedNotification>, IRequestHandler<GetDefaultMonitorRequest, string?>
{
	private readonly UserManager _userManager;
	private readonly IRequiredActor<UserSupervisorActor> _userSupervisor;
	public MonitorHandler(UserManager userManager, IRequiredActor<UserSupervisorActor> userSupervisor) {
		_userManager = userManager;
		_userSupervisor = userSupervisor;
	}

	public async Task Handle(MonitorOpenedNotification notification, CancellationToken cancellationToken) {
		var user = notification.User;
		if (user.IsGuest()) return;
		await _userManager.SaveLastViewedMonitorId(user.Name, notification.MonitorId);
		_userSupervisor.ActorRef.Tell(new ActorsApi.UpdateLastMonitor(user, notification.MonitorId));
	}

	public Task<string?> Handle(GetDefaultMonitorRequest request, CancellationToken cancellationToken) {
		return Task.FromResult(request.User.DefaultMonitorId);
	}
}
