using Microsoft.AspNetCore.SignalR;

namespace Cimon.Hubs;

public class UserHub : Hub
{
	public override Task OnConnectedAsync() {
		return base.OnConnectedAsync();
	}
}
