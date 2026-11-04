// Middleware.Core/Services/TransmissionRun.cs
using Microsoft.Extensions;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Eis;
using Middleware.Infra.Eis;
using Microsoft.Extensions.Options;

namespace Middleware.Core.Services;

public class TransmissionRun(
    IInvoiceRepository invoiceRepository,
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

        try
        {
            foreach (var invoice in invoices)
            {
                // Map invoice first before serializing
                var invoiceJson = EisJson.Serialize(eisMapper.Map(invoice));

                // Generate JWS
                invoice.AttachSignature(eisSigner.Sign(invoiceJson));

            }  

            var now = clock.Now;
            string submitId = SubmitIdGenerator.Create(options.Value.CertId, now);
            var transmission = Transmission.Create(submitId, invoices, now);

            

            // Submit Invoices
            await eisClient.SubmitBatch(submitId, invoices, ct);

            foreach (var invoice in invoices)
                // Mark invoice as queued
                invoice.MarkSubmitted();

            // Save the signatures before sending
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
       
        }
        catch
        {
            throw new InvalidOperationException("Something went wront ");
        }
    }
}