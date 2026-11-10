using Microsoft.Extensions.Options;
using Middleware.Core.Services;
using Middleware.Infra.Eis;

namespace Middleware.Api.Workers;

/// <summary>Runs ResultPollRun every ResultPoller:Interval to apply EIS per-invoice results.</summary>
public class EisResultPollerWorker(
    ILogger<EisResultPollerWorker> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<ResultPollerOptions> options
    ) : BackgroundService
{
    private static readonly TimeSpan DefaultInterval = TimeSpan.FromMinutes(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // A missing or zero interval would make this a tight loop against the EIS
        TimeSpan interval = options.Value.Interval > TimeSpan.Zero ? options.Value.Interval : DefaultInterval;
        logger.LogInformation("EIS result poller started; polling every {Interval}.", interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Scoped per iteration so the DbContext is released before the delay.
                // Created inside the try so DI/DB setup failures are logged too.
                await using AsyncServiceScope scope = serviceScopeFactory.CreateAsyncScope();
                ResultPollRun resultPollRun = scope.ServiceProvider.GetRequiredService<ResultPollRun>();
                await resultPollRun.ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, not an error
                break;
            }
            catch (Exception ex)
            {
                // Last-resort safety net: a failed poll must not stop the host. Retried next interval.
                logger.LogError(ex, "EIS result poll failed; will retry next interval.");
            }

            await Task.Delay(interval, stoppingToken);
        }

        logger.LogInformation("EIS result poller stopping.");
    }
}
