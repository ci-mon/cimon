using Cimon.Data.Common;
using Microsoft.AspNetCore.SignalR;

namespace Cimon.Internal;

public class HubAccessor<THub, TClient>(IHubContext<THub, TClient> hubContext) : IHubAccessor<TClient>
	where THub : Hub<TClient> where TClient : class
{
	public TClient Group(string name) => hubContext.Clients.Group(name);
}
