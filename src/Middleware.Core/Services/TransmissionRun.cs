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
        // Resend unfinished batches first (Created = never confirmed sent, Uncertain = timeout/5xx/network),
        // with their original submitId and stored signatures. If the EIS already has one it answers 409.
        // A resend that throws stops the run: no point building new batches while the EIS is unreachable.
        foreach (var pending in await transmissionRepository.FindUnfinishedAsync())
            await SendAsync(pending, ct);

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

        // Save BEFORE sending: the transmission (Created), its invoice links, the signatures and
        // the Submitting status, in one transaction. From here on, a failed send is picked up by
        // the resend loop above instead of being re-batched under a new submitId.
        await unitOfWork.SaveChangesAsync(CancellationToken.None);

        await SendAsync(transmission, ct);
    }

    /// <summary>
    /// Sends one transmission and records the outcome. Saves use CancellationToken.None because
    /// they record what already happened at the EIS and must not be skipped during shutdown.
    /// </summary>
    private async Task SendAsync(Transmission transmission, CancellationToken ct)
    {
        try
        {
            // A 2xx with an unreadable receipt throws here and leaves the transmission unchanged;
            // the resend gets 409 and marks it Sent (without a reference number)
            var receipt = await eisClient.SubmitBatch(transmission.SubmitId, transmission.Invoices, ct);
            var now = clock.Now;
            transmission.MarkSent(now);                          // sets SentAt
            transmission.Acknowledge(receipt.ReferenceNo, now);  // sets ResponseAt + ReferenceNo
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            transmission.MarkSent(clock.Now);               // EIS already has this submitId
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw;                                          // status unchanged; resent next cycle
        }
        catch (HttpRequestException ex) when (ex.StatusCode is >= HttpStatusCode.BadRequest and < HttpStatusCode.InternalServerError)
        {
            transmission.Fail(ex.Message);                  // permanent rejection: don't resend
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;                                          // worker logs it; needs a person to look at it
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            transmission.MarkUncertain(ex.Message);         // 5xx, timeout, network error
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            throw;                                          // worker logs it; resent next cycle
        }

        // Record Acknowledged (2xx) or Sent (409)
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
    }
}