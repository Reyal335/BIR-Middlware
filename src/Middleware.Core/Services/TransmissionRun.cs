// Middleware.Core/Services/TransmissionRun.cs
using Microsoft.Extensions;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;

namespace Middleware.Core.Services;

public class TransmissionRun(
    IInvoiceRepository invoiceRepository,
    IEisClient eisClient,
    IUnitOfWork unitOfWork,
    EisMapper eisMapper,
    IEisSigner eisSigner)
{
    public async Task ExecuteAsync(CancellationToken ct)
    {
        // Get due invoices
        List<Invoice> invoices = await invoiceRepository.FindQueuedInvoices();

        foreach (var invoice in invoices)
        {
            // Map invoice first before serializing
            var invoiceJson = EisJson.Serialize(eisMapper.Map(invoice));

            // Generate JWS
            invoice.AttachSignature(eisSigner.Sign(invoiceJson));
        }

        // Save the signatures before sending
        await unitOfWork.SaveChangesAsync(ct);

        await eisClient.SubmitBatch(invoices);
    }
}