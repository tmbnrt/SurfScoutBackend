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

    }
}
