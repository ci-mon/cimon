namespace Cimon.Shared;

public static class TimeProviderExtensions
{
	public static DateTime ToLocalDateTime(this BrowserDateTimeFormatProvider dateTimeFormatProvider, DateTime dateTime) {
		return dateTime.Kind switch {
			DateTimeKind.Unspecified => throw new InvalidOperationException(
				"Unable to convert unspecified DateTime to local time"),
			DateTimeKind.Local => dateTime,
			_ => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(dateTime, dateTimeFormatProvider.LocalTimeZone),
				DateTimeKind.Local),
		};
	}

	public static DateTime ToLocalDateTime(this BrowserDateTimeFormatProvider dateTimeFormatProvider, DateTimeOffset dateTime) {
		var local = TimeZoneInfo.ConvertTimeFromUtc(dateTime.UtcDateTime, dateTimeFormatProvider.LocalTimeZone);
		local = DateTime.SpecifyKind(local, DateTimeKind.Local);
		return local;
	}
}
