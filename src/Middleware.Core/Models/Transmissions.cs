namespace Middleware.Core.Models;

public class Transmission
{
    public Guid Id { get; private set; }
    public string SubmitId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ResponseAt { get; private set; }
    public TransmissionStatus Status { get; private set; }   // Created, Sent, Acknowledged, Failed, Uncertain
    public string? ReferenceNo { get; private set; }         // from the EIS receipt
    public string? Error { get; private set; }               // timeout, 401, etc.

    public List<Invoice> Invoices { get; private set; } = new();
}