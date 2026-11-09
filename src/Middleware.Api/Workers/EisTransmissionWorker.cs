using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Middleware.Core.Services;

namespace Middleware.Api.Workers;

public class EisTransmissionWorker : BackgroundService
{
    private readonly ILogger<EisTransmissionWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    // You can inject dependencies like loggers or HTTP clients here
    public EisTransmissionWorker(
        ILogger<EisTransmissionWorker> logger,
        IServiceScopeFactory serviceScopeFactory
        )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker Service started.");

        // Loop runs continuously in the background until the app shuts down
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

            try
            {
                // Scoped per iteration so the DbContext is released before the delay.
                // Created inside the try so DI/DB setup failures are logged too.
                await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();
                TransmissionRun transmissionRun = scope.ServiceProvider.GetRequiredService<TransmissionRun>();
                await transmissionRun.ExecuteAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Normal shutdown, not an error
                break;
            }
            catch (Exception ex)
            {
                // Last-resort safety net: a failed run must not stop the host. Retried next cycle.
                _logger.LogError(ex, "EIS transmission run failed; will retry next cycle.");
            }

            // Simulate background work (e.g., polling a database,   processing a queue)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
        }

        _logger.LogInformation("Worker Service stopping.");
    }
}
