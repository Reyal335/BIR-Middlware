using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Middleware.Core.interfaces;
using Middleware.Core.Models;

namespace Middleware.Api.Workers;

public class EisTransmissionWorker : BackgroundService
{
    private readonly ILogger<EisTransmissionWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    // You can inject dependencies like loggers or HTTP clients here
    public EisTransmissionWorker(ILogger<EisTransmissionWorker> logger, IServiceScopeFactory serviceScopeFactory)
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
            await using AsyncServiceScope scope = _serviceScopeFactory.CreateAsyncScope();

            IEisClient eisClient = scope.ServiceProvider.GetRequiredService<IEisClient>();
            IInvoiceRepository invoiceRepository = scope.ServiceProvider.GetRequiredService<IInvoiceRepository>();

            List<Invoice> invoices = await invoiceRepository.FindQueuedInvoices();

            await eisClient.SubmitBatch(invoices);

            // Simulate background work (e.g., polling a database,   processing a queue)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
        }

        _logger.LogInformation("Worker Service stopping.");
    }
}
