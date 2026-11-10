// Middleware.Core/Services/TransmissionRun.cs
using Microsoft.Extensions;
using System.Net;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;
using Middleware.Infra.Eis;
using Microsoft.Extensions.Options;

namespace Middleware.Core.Services;

public class TransmissionRun(
    IInvoiceRepository invoiceRepository,
    ITransmissionRepository transmissionRepository,
    IEisClient eisClient,
    IUnitOfWork unitOfWork,
    EisMapper eisMapper,
    IEisSigner eisSigner,
    IClock clock,
    IOptions<EisIdentityOptions> options
    )
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Get due invoices
        List<Invoice> invoices = await invoiceRepository.FindQueuedInvoices();

        // Nothing queued: don't send an empty batch (the EIS rejects 0 invoices)
        if (invoices.Count == 0)
            return;

        // Errors propagate to EisTransmissionWorker, which logs them and retries next cycle
        foreach (var invoice in invoices)
        {
            // Map invoice first before serializing
            var invoiceJson = EisJson.Serialize(eisMapper.Map(invoice));

            // Generate JWS
            invoice.AttachSignature(eisSigner.Sign(invoiceJson));
        }

        var now = clock.Now;
        string submitId = SubmitIdGenerator.Create(options.Value.CertificationNumber, now);
        var transmission = Transmission.Create(submitId, invoices, now);
        await transmissionRepository.Add(transmission);

        foreach (var invoice in invoices)
            // Mark invoice as submitted
            invoice.MarkSubmitted();

        // Submit Invoices
        await eisClient.SubmitBatch(submitId, invoices, ct);

        try
        {
            await eisClient.SubmitBatch(submitId, invoices, ct);
            transmission.MarkSent(clock.Now);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            transmission.MarkSent(clock.Now);               // EIS already has this submitId
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw;                                          // stays Created; resent next cycle
        }
        catch (HttpRequestException ex) when (ex.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
        {
            transmission.Fail(ex.Message);                  // permanent rejection
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            transmission.MarkUncertain(ex.Message);         // 5xx, timeout, network error
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;                                          // worker logs it
        }

        // Save the signatures and the new statuses
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}