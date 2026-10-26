using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;

namespace Middleware.Api.Workers;

public class EisTransmissionWorker : BackgroundService
{
    private readonly ILogger<EisTransmissionWorker> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly EisMapper _eisMapper;
    private readonly IEisSigner _eisSigner;
    // You can inject dependencies like loggers or HTTP clients here
    public EisTransmissionWorker(
        ILogger<EisTransmissionWorker> logger, 
        IServiceScopeFactory serviceScopeFactory,
        EisMapper eisMapper,
        IEisSigner eisSigner
        )
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _eisMapper = eisMapper;
        _eisSigner = eisSigner;
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

            // Get due invoices
            List<Invoice> invoices = await invoiceRepository.FindQueuedInvoices();
            foreach (var invoice in invoices) {

                // Map invoice first before serializing
                var invoiceJson = EisJson.Serialize(_eisMapper.Map(invoice));

                // Generate JWS
                invoice.AttachSignature(_eisSigner.Sign(invoiceJson));   
            }


            await eisClient.SubmitBatch(invoices);

            // Simulate background work (e.g., polling a database,   processing a queue)
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); 
        }

        _logger.LogInformation("Worker Service stopping.");
    }
}
