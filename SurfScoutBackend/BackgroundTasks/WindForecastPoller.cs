namespace SurfScoutBackend.BackgroundTasks
{
    public class WindForecastPoller : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await StoreForecastForPlannedSessionsAsync();

                // Calculate next full 4-hour interval
                var now = DateTime.UtcNow;
                var nextRunHour = ((now.Hour / 4) + 1) * 4 % 24;
                var nextRun = new DateTime(
                    now.Year, now.Month, now.Day,
                    nextRunHour, 0, 0, DateTimeKind.Utc);

                if (nextRun <= now)
                    nextRun = nextRun.AddDays(1);   // After 8 pm, schedule fot the next day

                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);
            }
        }

        private async Task StoreForecastForPlannedSessionsAsync()
        {
            // Get all planned sessions from the database
            // ...

            // TODO: Setup new table including foracast data (windspeed / direction)
            // for the available forecast models
            // ...

            // Open-meteo delivers forecast data in UTC time
            // --> Conversion to spot location required to store in local time
            // ...

            await Task.CompletedTask;
        }

    }
}
