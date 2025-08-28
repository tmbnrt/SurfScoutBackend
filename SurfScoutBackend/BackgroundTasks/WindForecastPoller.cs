using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using SurfScoutBackend.Utilities;
using SurfScoutBackend.Weather;
using System.Net.Http;

namespace SurfScoutBackend.BackgroundTasks
{
    public class WindForecastPoller : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly OpenMeteoWeatherClient _weatherClient_openmeteo;

        public WindForecastPoller(IServiceScopeFactory scopeFactory, OpenMeteoWeatherClient weatherClient_openmeteo)
        {
            // AppDbContext is scoped, WindForecastPoller is singleton.
            // Thus, creating a scope is necessaryto get an instance of AppDbContext.
            this._scopeFactory = scopeFactory;
            this._weatherClient_openmeteo = weatherClient_openmeteo;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Create new scope to get an instance of AppDbContext
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                await StoreForecastForPlannedSessionsAsync(context);

                // Delay logic: Calculate next full 3-hour interval
                var now = DateTime.UtcNow;

                // Next full 3-hour step
                var nextRunHour = ((now.Hour / 3) + 1) * 3;

                // If hour >= 24 --> next day
                var nextRunDay = nextRunHour >= 24 ? now.Date.AddDays(1) : now.Date;
                var nextRun = new DateTime(
                    nextRunDay.Year, nextRunDay.Month, nextRunDay.Day,
                    nextRunHour % 24, 0, 0, DateTimeKind.Utc);

                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task StoreForecastForPlannedSessionsAsync(AppDbContext context)
        {
            // Call all planned sessions from the database that are not in the past
            var plannedSessions = await context.plannedsessions
                .Where(ps => ps.Date >= DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();



            // Define wind models to be called from open-meteo
            string[] models = ["gfs", "icon", "ecmwf", "meteofrance_arome", "meteofrance_arpege", "noaa_hrrr", "dwd_icon_d2"];

            // For each planned session, call the weather API and store the forecast data
            foreach (var session in plannedSessions)
            {
                // get spot locations (longitude, latitude)
                var spot = await context.spots.FindAsync(session.SpotId);
                if (spot == null)
                    continue;

                var lng = spot.Location.X;
                var lat = spot.Location.Y;
                string timezone = TimeHelper.GetOpenMeteoTimezone(lng, lat);
                DateOnly date = session.Date;

                foreach (var model in models)
                {
                    var url = $"https://api.open-meteo.com/v1/forecast?latitude={lat}&longitude={lng}&hourly=wind_speed_10m,wind_direction_10m&model={model}";

                    // Call open-meteo API
                    var windData = await _weatherClient_openmeteo.ForcastDataByModelAsync(lng, lat, timezone, date, model);
                    if (windData.IsNullOrEmpty())
                        continue;

                    // Store wind data in the database
                    // ... CHALLENGE: windData contains the full day hourly --> plannedSessions can contain multiple sessions by different users.
                    // The table should include a list for 17 timestamps (from 6am to 10pm)
                    // TODO: reduce windData to relevant timestamps only
                }

                // Open-meteo delivers forecast data in UTC time
                // --> Conversion to spot location required to store in local time
                // ...

                // table including foracast data (windspeed / direction): windforecasts
                // ...
            }






            await Task.CompletedTask;
        }

    }
}
