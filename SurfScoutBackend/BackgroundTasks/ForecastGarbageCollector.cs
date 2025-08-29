using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SurfScoutBackend.Data;
using SurfScoutBackend.Weather;

namespace SurfScoutBackend.BackgroundTasks
{
    public class ForecastGarbageCollector : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public ForecastGarbageCollector(IServiceScopeFactory scopeFactory)
        {
            // AppDbContext is scoped, WindForecastPoller is singleton.
            // Thus, creating a scope is necessaryto get an instance of AppDbContext.
            this._scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Create new scope to get an instance of AppDbContext
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                // Cleanup old forecasts
                await CleanupOldForecastsAsync(context, now);

                // Delay logic: repeat on the next day
                var nextRunDate = now.Date.AddDays(1);
                var nextRun = new DateTime(
                    nextRunDate.Year, nextRunDate.Month, nextRunDate.Day,
                    0, 0, 0, DateTimeKind.Utc);

                var delay = nextRun - now;

                // Wait for the delay or until the stopping token is triggered
                await Task.Delay(delay, stoppingToken);
            }
        }

        // Function to cleanup forecasts. But keep the forecasts for the rated sessions a the specific time.
        private async Task CleanupOldForecastsAsync(AppDbContext context, DateTime now)
        {
            // TODO: Cleanup logic
            // ...
        }
    }
}
