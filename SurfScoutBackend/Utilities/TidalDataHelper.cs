using SurfScoutBackend.Models;

namespace SurfScoutBackend.Utilities
{
    public static class TidalDataHelper
    {
        public static string GetSessionTideAsString(List<TideData> tideExtremes,
                                                DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var sessionMidTime = date.ToDateTime(startTime) + (date.ToDateTime(endTime) - date.ToDateTime(startTime)) / 2;

            var dayTides = tideExtremes
                .Where(t => DateOnly.FromDateTime(t.Timestamp) == date)
                .OrderBy(t => t.Timestamp)
                .ToList();

            // Check if time is next to extreme points
            foreach (var tide in dayTides)
            {
                var minutesDiff = Math.Abs((tide.Timestamp - sessionMidTime).TotalMinutes);

                if (tide.Type.ToLower() == "high" && minutesDiff <= 120)
                    return "high tide";

                if (tide.Type.ToLower() == "low" && minutesDiff <= 120)
                    return "low tide";
            }

            // Check in between extreme points
            for (int i = 0; i < dayTides.Count - 1; i++)
            {
                var current = dayTides[i];
                var next = dayTides[i + 1];

                if (sessionMidTime > current.Timestamp && sessionMidTime < next.Timestamp)
                {
                    bool rising = current.Type.ToLower() == "low" && next.Type.ToLower() == "high";
                    bool falling = current.Type.ToLower() == "high" && next.Type.ToLower() == "low";

                    if (rising) return "rising mid tide";
                    if (falling) return "falling mid tide";
                }
            }

            return "unknown tide";
        }
    }
}
