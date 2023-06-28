using Cimon.Contracts;
using MediatR;

namespace Cimon.Data.Users;


public record MonitorOpenedNotification(string MonitorId) : INotification;

public record GetDefaultMonitorRequest : IRequest<string?>, IRequest;


class MonitorHandler : INotificationHandler<MonitorOpenedNotification>, IRequestHandler<GetDefaultMonitorRequest, string?>
{
	private readonly UserManager _userManager;
	private readonly ICurrentUserAccessor _currentUserAccessor;
	public MonitorHandler(UserManager userManager, ICurrentUserAccessor currentUserAccessor) {
		_userManager = userManager;
		_currentUserAccessor = currentUserAccessor;
	}

	public async Task Handle(MonitorOpenedNotification notification, CancellationToken cancellationToken) {
		var user = await _currentUserAccessor.Current;
		if (user.IsGuest()) return;
		await _userManager.SaveLastViewedMonitorId(user.Name, notification.MonitorId);
	}

	public async Task<string?> Handle(GetDefaultMonitorRequest request, CancellationToken cancellationToken) {
		var user = await _currentUserAccessor.Current;
		return user.DefaultMonitorId;
	}
}
