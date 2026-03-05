using GlobalActivityBot.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GlobalActivityBot.BackgroundJobs;

public class GlobalXpSyncJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<GlobalXpSyncJob> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(5);

    public GlobalXpSyncJob(IServiceProvider serviceProvider, ILogger<GlobalXpSyncJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("GlobalXpSyncJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var statsService = scope.ServiceProvider.GetRequiredService<IStatsService>();
                await statsService.SyncGlobalXpAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error during global XP sync");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
