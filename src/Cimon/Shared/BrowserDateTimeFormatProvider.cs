using System.Globalization;

namespace Cimon.Shared;

public static class  BrowserTimeServiceCollectionExtensions
{
	public static IServiceCollection AddBrowserTimeProvider(this IServiceCollection services)
		=> services.AddScoped<BrowserDateTimeFormatProvider, BrowserDateTimeFormatProvider>();
}

public sealed class BrowserDateTimeFormatProvider : TimeProvider
{
	private TimeZoneInfo? _browserLocalTimeZone;
	private CultureInfo? _browserCulture;

	public CultureInfo BrowserCulture {
		get => _browserCulture ?? CultureInfo.CurrentCulture;
		private set => _browserCulture = value;
	}

	public event EventHandler? LocalTimeZoneChanged;

	public override TimeZoneInfo LocalTimeZone
		=> _browserLocalTimeZone ?? base.LocalTimeZone;

	internal bool IsLocalTimeZoneSet => _browserLocalTimeZone != null;

	public void SetBrowserTimeZone(string timeZone) {
		if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var timeZoneInfo)) {
			timeZoneInfo = null;
		}
		if (timeZoneInfo?.Equals(LocalTimeZone) == true) return;
		_browserLocalTimeZone = timeZoneInfo;
		LocalTimeZoneChanged?.Invoke(this, EventArgs.Empty);
	}

	public void SetBrowserOptions(BrowserDateTimeFormatOptions options) {
		CultureInfo cultureInfo = null;
		try {
			if (options.Locale is not null && CultureInfo.GetCultureInfo(options.Locale) is { } info) {
				cultureInfo = (CultureInfo)info.Clone();
				BrowserCulture = cultureInfo;
				CultureInfo.CurrentCulture = cultureInfo;
				CultureInfo.CurrentUICulture = cultureInfo;
			}
		} catch {
			// ignored
		}
		SetBrowserTimeZone(options.TimeZone);
	}
}
