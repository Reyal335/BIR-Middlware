using Middleware.Core.Eis;
using Middleware.Core.Models;

namespace Middleware.Core.interfaces;

public interface IEisClient
{
    /// <summary>Sends the batch and returns the decrypted receipt. Throws HttpRequestException on a non-2xx response.</summary>
    Task<EisReceipt> SubmitBatch(string SubmitId, List<Invoice> invoice, CancellationToken ct);

    Task GetResults();
}