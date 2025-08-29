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

                var now = DateTime.UtcNow;

                await StoreForecastForPlannedSessionsAsync(context, now);

                // Delay logic: Calculate next full 3-hour interval
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

        private async Task StoreForecastForPlannedSessionsAsync(AppDbContext context, DateTime requestTime)
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
                // Get spot locations (longitude, latitude)
                var spot = await context.spots.FindAsync(session.SpotId);
                if (spot == null)
                    continue;

                var lng = spot.Location.X;
                var lat = spot.Location.Y;
                string timezone = TimeHelper.GetOpenMeteoTimezone(lng, lat);
                DateOnly date = session.Date;

                foreach (var model in models)
                {
                    // Call open-meteo API
                    var windData = await _weatherClient_openmeteo.ForcastDataByModelAsync(lng, lat, timezone, date, model);
                    if (windData.IsNullOrEmpty())
                        continue;

                    // Delete the first 6 entries and the last entry to reduce data to 6am to 10pm only
                    windData.RemoveRange(0, 6);
                    windData.RemoveAt(windData.Count - 1);

                    foreach (var dataset in windData)
                    {
                        // add to the windforecasts table
                        var windForecast = new WindForecast
                        {
                            SessionId = session.Id,
                            RequestTime = requestTime,
                            Timestamp = dataset.Timestamp,
                            WindspeedKnots = dataset.SpeedInKnots,
                            Direction = dataset.DirectionInDegrees,
                            Model = model
                        };
                        context.windforecasts.Add(windForecast);
                    }
                }

                await context.SaveChangesAsync();
            }

            await Task.CompletedTask;
        }

    }
}
