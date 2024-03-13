using System.Web;
using Cimon.Data.Users;
using MediatR;
using Microsoft.JSInterop;

namespace Cimon.Monitors;

public class SavedMonitorInLocalStoragesBehaviour : IPipelineBehavior<GetDefaultMonitorRequest, string?>, 
	INotificationHandler<MonitorOpenedNotification>
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly IJSRuntime _jsRuntime;
	private const string Key = "DefaultMonitorId";

	public SavedMonitorInLocalStoragesBehaviour(IHttpContextAccessor httpContextAccessor, IJSRuntime jsRuntime) {
		_httpContextAccessor = httpContextAccessor;
		_jsRuntime = jsRuntime;
	}

	public async Task<string?> Handle(GetDefaultMonitorRequest request, RequestHandlerDelegate<string?> next, 
			CancellationToken cancellationToken) {
		var httpContext = _httpContextAccessor.HttpContext;
		var monitorId = await next();
		if (!string.IsNullOrWhiteSpace(monitorId) || httpContext is null) return monitorId;
		if (httpContext.Request.Cookies.TryGetValue(Key, out var result)) {
			return HttpUtility.UrlDecode(result);
		}
		if (httpContext.Items.TryGetValue(Key, out var item) && item is string monitor) {
			return monitor;
		}
		return null;
	}

	public async Task Handle(MonitorOpenedNotification notification, CancellationToken cancellationToken) {
		var httpContext = _httpContextAccessor.HttpContext;
		if (httpContext is null) return;
		var currentValue = await Handle(new GetDefaultMonitorRequest(notification.User),
			() => Task.FromResult<string>(null), cancellationToken);
		if (currentValue == notification.MonitorId) {
			return;
		}
		var encoded = HttpUtility.UrlEncode(notification.MonitorId) ?? string.Empty;
		if (httpContext.WebSockets.IsWebSocketRequest) {
			await _jsRuntime.InvokeAsync<object>("blazorExtensions.WriteCookie", cancellationToken, Key, encoded);
		} else {
			httpContext.Response.Cookies.Append(Key, encoded);
		}
		httpContext.Items[Key] = notification.MonitorId;
	}
}
