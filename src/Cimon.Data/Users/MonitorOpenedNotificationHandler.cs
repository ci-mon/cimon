using Akka.Actor;
using Cimon.Contracts;
using MediatR;

namespace Cimon.Data.Users;


public record MonitorOpenedNotification(User User, string? MonitorId) : INotification;

public record GetDefaultMonitorRequest(User User) : IRequest<string?>, IRequest;


class MonitorHandler : INotificationHandler<MonitorOpenedNotification>, IRequestHandler<GetDefaultMonitorRequest, string?>
{
	private readonly UserManager _userManager;
	public MonitorHandler(UserManager userManager) {
		_userManager = userManager;
	}

	public async Task Handle(MonitorOpenedNotification notification, CancellationToken cancellationToken) {
		var user = notification.User;
		if (user.IsGuest()) return;
		await _userManager.SaveLastViewedMonitorId(user.Name, notification.MonitorId);
		AppActors.Instance.UserSupervisor.Tell(new ActorsApi.UpdateLastMonitor(user, notification.MonitorId));
	}

	public Task<string?> Handle(GetDefaultMonitorRequest request, CancellationToken cancellationToken) {
		return Task.FromResult(request.User.DefaultMonitorId);
	}
}
