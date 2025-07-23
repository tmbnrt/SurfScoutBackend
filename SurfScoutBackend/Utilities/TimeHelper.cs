using GeoTimeZone;
using TimeZoneConverter;

namespace SurfScoutBackend.Utilities
{
    public static class TimeHelper
    {
        public static TimeOnly GetMeanTime(TimeOnly a, TimeOnly b)
        {
            int seconds_a = a.ToTimeSpan().Seconds + a.ToTimeSpan().Minutes * 60 + a.ToTimeSpan().Hours * 3600;
            int seconds_b = b.ToTimeSpan().Seconds + b.ToTimeSpan().Minutes * 60 + b.ToTimeSpan().Hours * 3600;

            int meanSeconds = (seconds_a + seconds_b) / 2;

            TimeOnly meanTime = TimeOnly.FromTimeSpan(TimeSpan.FromSeconds(meanSeconds));

            return meanTime;
        }

        public static DateTime ToUtc(DateOnly date, TimeOnly time, double lat, double lng)
        {
            var localTime = date.ToDateTime(time);

            // Time zone from coordinates
            string ianaZone = TimeZoneLookup.GetTimeZone(lat, lng).Result;

            // Convert to Dotnet timezone
            TimeZoneInfo tz = TZConvert.GetTimeZoneInfo(ianaZone);

            // Local time to UTC
            return TimeZoneInfo.ConvertTimeToUtc(localTime, tz);
        }
    }
}
