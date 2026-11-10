// Middleware.Core/Services/ResultPollRun.cs
using Microsoft.Extensions.Logging;
using Middleware.Core.Eis;
using Middleware.Core.interfaces;
using Middleware.Core.Models;

namespace Middleware.Core.Services;

/// <summary>
/// Second round trip: asks the EIS Inquiry Result API about every Sent/Acknowledged transmission
/// and applies the per-invoice results once the batch is COMPLETED.
/// </summary>
public class ResultPollRun(
    ITransmissionRepository transmissionRepository,
    IEisClient eisClient,
    IUnitOfWork unitOfWork,
    IClock clock,
    ILogger<ResultPollRun> logger
    )
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Errors other than "not found" propagate to EisResultPollerWorker, which logs them and
        // tries again next interval. Transmissions after the failing one wait until then.
        foreach (var transmission in await transmissionRepository.FindAwaitingResultsAsync())
        {
            EisInquiryResult result;
            try
            {
                result = await eisClient.GetResults(transmission.SubmitId, ct);
            }
            catch (EisSubmissionNotFoundException ex)
            {
                // The EIS has no record of this batch (e.g. the mock restarted and lost its memory).
                // Uncertain makes TransmissionRun resend it with the same submitId and stored signatures.
                logger.LogWarning(ex, "EIS has no record of transmission {SubmitId}; marking it for resend.", transmission.SubmitId);
                transmission.MarkUncertain(ex.Message);
                await unitOfWork.SaveChangesAsync(CancellationToken.None);
                continue;
            }

            // Still validating: ask again next interval
            if (result.Status != EisBatchStatus.Completed)
                continue;

            ApplyResults(transmission, result);

            // Records results the EIS already decided; must not be skipped during shutdown
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }
    }

    private void ApplyResults(Transmission transmission, EisInquiryResult result)
    {
        var now = clock.Now;

        // Matched by unique ID, not position: a resend loads invoices in no guaranteed order,
        // so the positions the EIS stored may not match this transmission's invoice order
        var byUniqueId = result.Results
            .Where(r => r.UniqueId is not null)
            .GroupBy(r => r.UniqueId!)
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var invoice in transmission.Invoices)
        {
            if (!byUniqueId.TryGetValue(invoice.EisUniqueId, out var invoiceResult))
            {
                // Shouldn't happen: the middleware built every JWS itself. Left in Submitting for a person to check.
                logger.LogError(
                    "EIS returned no result for invoice {EisUniqueId} ({InvoiceNumber}) in transmission {SubmitId}; left in {Status}.",
                    invoice.EisUniqueId, invoice.InvoiceNumber, transmission.SubmitId, invoice.Status);
                continue;
            }

            if (invoice.Status != InvoiceStatus.Submitting)
            {
                logger.LogWarning(
                    "Invoice {EisUniqueId} in transmission {SubmitId} is already {Status}; EIS result {Code} not applied.",
                    invoice.EisUniqueId, transmission.SubmitId, invoice.Status, invoiceResult.Code);
                continue;
            }

            invoice.ApplyEisResult(invoiceResult.Code, invoiceResult.Message, now);
        }

        transmission.Complete(now);
    }
}
