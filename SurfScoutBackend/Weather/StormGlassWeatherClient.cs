﻿using SurfScoutBackend.Utilities;
using SurfScoutBackend.Models;
using System.Net.Http.Headers;
using GeoTimeZone;
using TimeZoneConverter;
using System.Text.Json;

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
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        }

        public async Task<List<WindData>> GetWindAsync(double lat, double lng, DateOnly date, TimeOnly startTime, TimeOnly endTime)
        {
            var startUtc = TimeHelper.ToUtc(date, startTime, lat, lng);
            var endUtc = TimeHelper.ToUtc(date, endTime, lat, lng);

            var isoStart = startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");
            var isoEnd = endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ");

            // Call StormGlass API
            var url = $"https://api.stormglass.io/v2/weather/point?lat={lat}&lng={lng}&params=windSpeed,windDirection&start={isoStart}&end={isoEnd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Stormglass API error: {response.StatusCode}");
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
                        Timestamp = time,
                        SpeedInKnots = windSpeedProp.GetProperty("sg").GetDouble(),
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

            // Call StormGlass API
            var url = $"https://api.stormglass.io/v2/tide/point?lat={lat}&lng={lng}&start={isoStart}&end={isoEnd}";
            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Stormglass API error: {response.StatusCode}");
            }

            var json = await response.Content.ReadAsStringAsync();

            // Parse JSON
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var hours = root.GetProperty("hours");

            // Dynamic time zone based on coordinates
            var timeZoneId = TimeZoneLookup.GetTimeZone(lat, lng).Result;
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);

            var tideDataList = new List<TideData>();

            foreach (var hour in hours.EnumerateArray())
            {
                if (hour.TryGetProperty("type", out var typeProp) &&
                    hour.TryGetProperty("height", out var heightProp) &&
                    hour.TryGetProperty("timestamp", out var timeProp))
                {
                    var type = typeProp.GetString();
                    if (type == "high" || type == "low")
                    {
                        var utcTime = DateTimeOffset.FromUnixTimeSeconds(timeProp.GetInt64()).UtcDateTime;
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
