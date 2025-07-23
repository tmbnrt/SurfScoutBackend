using SurfScoutBackend.Models;
using System.Diagnostics.Metrics;

namespace SurfScoutBackend.Utilities
{
    public static class WeatherDataHelper
    {
        public static double AverageWindSpeed(List<WindData> windData)
        {
            double speedSum = 0;

            int counter = 0;
            foreach (WindData set in windData)
            {
                speedSum = speedSum + set.SpeedInKnots;
                counter++;
            }

            return Math.Round((speedSum / counter), 1);
        }

        public static double AverageWindDirectionDegree(List<WindData> windData)
        {
            double dirSum = 0;

            int counter = 0;
            foreach (WindData set in windData)
            {
                dirSum = dirSum + set.DirectionInDegrees;
                counter++;
            }

            return Math.Round((dirSum / counter), 1);
        }
    }
}
