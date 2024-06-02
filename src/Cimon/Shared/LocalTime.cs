using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace Cimon.Shared;

public sealed class LocalTime : ComponentBase, IDisposable
{
	[Inject] public BrowserDateTimeFormatProvider DateTimeFormatProvider { get; set; } = default!;

	[Parameter] public DateTime? DateTime { get; set; }
	[Parameter] public string? Format { get; set; }

	protected override void OnInitialized() {
		if (DateTimeFormatProvider is { } browserTimeProvider) {
			browserTimeProvider.LocalTimeZoneChanged += LocalTimeZoneChanged;
		}
	}

	protected override void BuildRenderTree(RenderTreeBuilder builder) {
		if (DateTime == null) return;
		var localDateTime = DateTimeFormatProvider.ToLocalDateTime(DateTime.Value);
		var text = Format is not null
			? localDateTime.ToString(Format, DateTimeFormatProvider.BrowserCulture)
			: localDateTime.ToString(DateTimeFormatProvider.BrowserCulture);
		builder.AddContent(0, text);
	}

	public void Dispose() {
		if (DateTimeFormatProvider is { } browserTimeProvider) {
			browserTimeProvider.LocalTimeZoneChanged -= LocalTimeZoneChanged;
		}
	}

	private void LocalTimeZoneChanged(object? sender, EventArgs e) {
		_ = InvokeAsync(StateHasChanged);
	}
}
