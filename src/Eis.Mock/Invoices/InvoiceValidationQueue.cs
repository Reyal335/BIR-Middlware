using System.Threading.Channels;

namespace Eis.Mock.Invoices;

/// <summary>Submission IDs waiting for per-invoice validation.</summary>
public class InvoiceValidationQueue
{
    private readonly Channel<string> channel = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = true });

    public void Enqueue(string submitId) => channel.Writer.TryWrite(submitId);

    public IAsyncEnumerable<string> ReadAllAsync(CancellationToken ct) => channel.Reader.ReadAllAsync(ct);
}
