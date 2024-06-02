using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;

namespace Cimon.Shared;

public class BrowserDateTimeFormatOptions
{
	[JsonProperty("timeZone")]
	public string TimeZone { get; set; }
	//[JsonProperty("locale")]
	public string? Locale { get; set; }
}

public sealed class InitializeDateTimeFormat : ComponentBase
{
	[Inject] public BrowserDateTimeFormatProvider BrowserDateTimeFormatProvider { get; set; } = default!;
	[Inject] public IJSRuntime JSRuntime { get; set; } = default!;

	protected override async Task OnAfterRenderAsync(bool firstRender) {
		if (firstRender && BrowserDateTimeFormatProvider is { IsLocalTimeZoneSet: false } browserTimeProvider) {
			try {
				await using var module = await JSRuntime.InvokeAsync<IJSObjectReference>("import", "./js/timezone.js");
				var options = await module.InvokeAsync<BrowserDateTimeFormatOptions>("getBrowserTimeZone");
				browserTimeProvider.SetBrowserOptions(options);
			} catch (JSDisconnectedException) {
			}
		}
	}
}
