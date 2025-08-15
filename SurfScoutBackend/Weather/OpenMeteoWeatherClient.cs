using SurfScoutBackend.Models;
using SurfScoutBackend.Models.WindFieldModel;
using NetTopologySuite.Geometries;
using SurfScoutBackend.Utilities;
using System;
using System.Text.Json;
using System.Globalization;

namespace SurfScoutBackend.Weather
{
    public class OpenMeteoWeatherClient
    {
        private readonly HttpClient _httpClient;

        public OpenMeteoWeatherClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<WindField>> GetWindFieldAsync(Spot spot, int sessionId, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            List<WindField> windFieldHistory = new List<WindField>();

            // Calculate data points inside windfield polygon >>> Spacing is 25km (related to frontend)
            List<Point> points = GeoDataHelper.GenerateRasterPointsInPolygon(spot.WindFetchPolygon, 25000.0);

            // Set the time steps
            int timeStep_hours = 3;
            int numberOfSteps = 4;

            // Create a list of time points for wind field history
            TimeOnly sessionMeanTime = TimeHelper.GetMeanTime(startTime, endTime);
            TimeOnly sessionTime = sessionMeanTime.Minute > 0 || sessionMeanTime.Second > 0
                ? new TimeOnly((sessionMeanTime.Hour + 1) % 24, 0)
                : sessionMeanTime;

            List<string> timePoint_string = new List<string>();
            for (int i = numberOfSteps; i >= 0; i--)
            {
                TimeOnly timePoint = sessionTime.AddHours(-timeStep_hours * i);
                string timestampOpenMeteo = TimeHelper.GetOpenMeteoTimeStamp(date, timePoint);

                // Add to time date if the time point is on the same date as the session date
                if (date.ToDateTime(timePoint).Date == date.ToDateTime(TimeOnly.MinValue).Date)
                {
                    timePoint_string.Add(TimeHelper.GetOpenMeteoTimeStamp(date, timePoint));
                    windFieldHistory.Add(new WindField
                    {
                        Timestamp = timePoint,
                        Date = date,
                        SessionId = sessionId,
                        Session = null!,                            // Navigation property, will be set later
                        Points = new List<WindFieldPoint>()         // Initialize empty list for points
                    });
                }                    
            }

            // Strings for open-meteo API call
            string formattedDate = date.ToString("yyyy-MM-dd");
            string timezone = TimeHelper.GetOpenMeteoTimezone(spot.Location.X, spot.Location.Y);

            // TODO: Problem: iterating ove rpoints first, but points are inner instances. Timestamp is outer instance
            foreach (var point in points)
            {
                string lng = point.X.ToString(CultureInfo.InvariantCulture);
                string lat = point.Y.ToString(CultureInfo.InvariantCulture);                

                var url = $"https://api.open-meteo.com/v1/forecast?" +
                          $"latitude={lat}&longitude={lng}" +
                          $"&hourly=wind_speed_10m,wind_direction_10m" +
                          $"&start_date={formattedDate}&end_date={formattedDate}" +
                          $"&timezone={timezone}";
                
                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                // Iterate time points and create historic data
                int time_index = 0;
                foreach (string time in timePoint_string)
                {
                    // Parse JSON
                    using var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;
                    var hourly = root.GetProperty("hourly");
                    var timeArray = hourly.GetProperty("time");
                    var windSpeedArray = hourly.GetProperty("wind_speed_10m");
                    var windDirectionArray = hourly.GetProperty("wind_direction_10m");
                    // Find the index of the time point
                    int index = Array.IndexOf(timeArray.EnumerateArray().Select(t => t.GetString()).ToArray(), time);
                    if (index >= 0)
                    {
                        double windSpeed_knots = windSpeedArray[index].GetDouble() * 0.53996;   // Convert kmh in knots
                        double windDirection_degree = windDirectionArray[index].GetDouble();

                        windFieldHistory[time_index].Points.Add(new WindFieldPoint
                        {
                            WindSpeedKnots = windSpeed_knots,
                            WindDirectionDegree = windDirection_degree,
                            Location = new Point(point.X, point.Y) { SRID = 4326 },
                            WindFieldId = windFieldHistory[time_index].Id,          // Set the foreign key
                            WindField = windFieldHistory[time_index]                // Navigation property
                        });
                    }

                    time_index++;
                }
            }

            return windFieldHistory;
        }
    }
}
