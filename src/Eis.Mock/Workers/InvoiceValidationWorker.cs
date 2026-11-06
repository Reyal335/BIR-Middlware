using Eis.Mock.Invoices;

namespace Eis.Mock.Workers;

/// <summary>Validates each accepted batch's invoices after its receipt has been returned.</summary>
public class InvoiceValidationWorker(
    InvoiceValidationQueue queue,
    InvoiceValidationService validationService,
    ILogger<InvoiceValidationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (string submitId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                validationService.Process(submitId);
            }
            catch (Exception ex)
            {
                // Leaves the batch at RECEIVED; keep processing the rest of the queue
                logger.LogError(ex, "Invoice validation failed for submission {SubmitId}", submitId);
            }
        }
    }
}
