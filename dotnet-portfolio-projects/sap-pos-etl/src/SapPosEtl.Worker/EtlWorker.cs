using Microsoft.Extensions.Options;

namespace SapPosEtl.Worker;

public class EtlWorker(IServiceScopeFactory scopeFactory, IOptions<EtlOptions> options, ILogger<EtlWorker> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(Math.Max(1, options.Value.IntervalMinutes));
        using var timer = new PeriodicTimer(interval);

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var runner = scope.ServiceProvider.GetRequiredService<EtlRunner>();
                var summary = await runner.RunAsync(DateOnly.FromDateTime(DateTime.Now), stoppingToken);
                logger.LogInformation(
                    "ETL run finished: {Extracted} extracted, {Loaded} loaded, {Skipped} already synced, {Invalid} invalid, {Failed} failed",
                    summary.Extracted, summary.Loaded, summary.Skipped, summary.Invalid, summary.Failed);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "ETL run failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
