using SurfScoutBackend.Utilities;
using SurfScoutBackend.Models;
using System.Net.Http.Headers;
using GeoTimeZone;
using TimeZoneConverter;
using System.Text.Json;
using System.Net;
using System.Globalization;

namespace SurfScoutBackend.Weather
{
    public class StormglassWeatherClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public StormglassWeatherClient(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_apiKey);
        }

        public async Task<List<WindData>> GetWindAsync(double lat, double lng, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var startUtc = TimeHelper.ToUtc(date, startTime, lat, lng);
            var endUtc = TimeHelper.ToUtc(date, endTime, lat, lng);

            string isoStart = startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            string isoEnd = endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string lat_str = lat.ToString(CultureInfo.InvariantCulture);
            string lng_str = lng.ToString(CultureInfo.InvariantCulture);

            // Call StormGlass API
            var url = $"https://api.stormglass.io/v2/weather/point?lat={lat_str}&lng={lng_str}&params=windSpeed,windDirection&start={isoStart}&end={isoEnd}";
            var response = await _httpClient.GetAsync(url);

            if ((int)response.StatusCode == 422)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stormglass API error: Unprocessable Entity – {errorContent}");
            }

            if ((int)response.StatusCode == 403)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stormglass API error: Unprocessable Entity – {errorContent}");
            }

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stormglass API error: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            // Parse JSON
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var hours = root.GetProperty("hours");

            var windDataList = new List<WindData>();

            foreach (var hour in hours.EnumerateArray())
            {
                if (hour.TryGetProperty("windSpeed", out var windSpeedProp) &&
                hour.TryGetProperty("windDirection", out var windDirProp) &&
                hour.TryGetProperty("time", out var timeProp))
                {
                    var time = DateTime.Parse(timeProp.GetString()!).ToUniversalTime();

                    // Data sources: sg(stormglass: automatically worldwide)  dwd:middle Europe  ukMetOffice: UK/Northsea
                    windDataList.Add(new WindData
                    {
                        Timestamp = time,       // Time in UTC (European summer time --> MESZ: UTC+2)
                        SpeedInKnots = windSpeedProp.GetProperty("sg").GetDouble() * 1.94384,
                        DirectionInDegrees = windDirProp.GetProperty("sg").GetDouble()
                    });
                }
            }

            return windDataList;
        }

        public async Task<List<TideData>> GetTideExtremesAsync(double lat, double lng, DateOnly date)
        {
            var startUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue)).UtcDateTime;
            var endUtc = new DateTimeOffset(date.AddDays(1).ToDateTime(TimeOnly.MinValue)).UtcDateTime;

            var isoStart = startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var isoEnd = endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            string lat_str = lat.ToString(CultureInfo.InvariantCulture);
            string lng_str = lng.ToString(CultureInfo.InvariantCulture);

            // Call StormGlass API
            var url = $"https://api.stormglass.io/v2/tide/extremes/point?lat={lat_str}&lng={lng_str}&start={isoStart}&end={isoEnd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Stormglass API error: {errorContent}");
            }

            var json = await response.Content.ReadAsStringAsync();

            // Parse JSON
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var data = root.GetProperty("data");

            // Dynamic time zone based on coordinates
            var timeZoneId = TimeZoneLookup.GetTimeZone(lat, lng).Result;
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            var tideDataList = new List<TideData>();

            foreach (var hour in data.EnumerateArray())
            {
                if (hour.TryGetProperty("type", out var typeProp) &&
                    hour.TryGetProperty("height", out var heightProp) &&
                    hour.TryGetProperty("time", out var timeProp))
                {
                    var type = typeProp.GetString();
                    if (type == "high" || type == "low")
                    {
                        var timeString = timeProp.GetString();
                        var utcTime = DateTimeOffset.Parse(timeString!).UtcDateTime;
                        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZoneInfo);

                        tideDataList.Add(new TideData
                        {
                            Timestamp = localTime,
                            Type = type,
                            HeightInMeters = heightProp.GetDouble()
                        });
                    }
                }
            }

            return tideDataList;
        }
    }
}
