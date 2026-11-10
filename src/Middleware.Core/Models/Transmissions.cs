namespace Middleware.Core.Models;

public class Transmission
{
    public Guid Id { get; private set; }
    public string SubmitId { get; private set; } = default!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? ResponseAt { get; private set; }
    public TransmissionStatus Status { get; private set; }   // Created, Sent, Acknowledged, Failed, Uncertain, Completed
    public string? ReferenceNo { get; private set; }         // from the EIS receipt
    public string? Error { get; private set; }               // timeout, 401, etc.
    public DateTimeOffset? CompletedAt { get; private set; } // when per-invoice results were applied

    public List<Invoice> Invoices { get; private set; } = new();

    public static Transmission Create(string submitId, List<Invoice> invoices, DateTimeOffset now) => new()
    {
        Id = Guid.NewGuid(),
        SubmitId = submitId,
        CreatedAt = now,
        Status = TransmissionStatus.Created,
        Invoices = invoices
    };

    public void MarkSent(DateTimeOffset now)
    {
        Status = TransmissionStatus.Sent;
        SentAt = now;
    }
    public void Acknowledge(string referenceNo, DateTimeOffset now)
    {
        Status = TransmissionStatus.Acknowledged;
        ResponseAt = now;
        ReferenceNo = referenceNo;
    }
    public void MarkUncertain(string message)
    {
        Status = TransmissionStatus.Uncertain;
        Error = message;
    }
    public void Fail(string message)
    {
        Status = TransmissionStatus.Failed;
        Error = message;
    }

    // Per-invoice results have been applied; the result poller stops asking about this batch
    public void Complete(DateTimeOffset now)
    {
        if (Status is not (TransmissionStatus.Sent or TransmissionStatus.Acknowledged))
            throw new InvalidOperationException($"Cannot complete transmission from {Status}");
        Status = TransmissionStatus.Completed;
        CompletedAt = now;
    }
}