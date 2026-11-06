using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Eis.Mock.Store;

public enum SubmissionStatus { RECEIVED, COMPLETED }

/// <summary>Outcome of one invoice in a batch. Position is 1-based, in the order the JWS strings were sent.</summary>
public sealed record InvoiceResult(int Position, string? UniqueId, string Code, string Message);

public sealed class Submission
{
    public required string SubmitId { get; init; }
    public required string ReferenceNo { get; init; }

    /// <summary>Mock clock (Manila time) when the batch was accepted.</summary>
    public required DateTime ReceivedAt { get; init; }

    /// <summary>Middleware clock (Manila time) taken from the request's datetime header.</summary>
    public required DateTime TransmittedAt { get; init; }

    public required int InvoiceCount { get; init; }
    public required IReadOnlyList<string> Jws { get; init; }

    private volatile SubmissionStatus _status = SubmissionStatus.RECEIVED;
    private IReadOnlyDictionary<string, InvoiceResult> _results = new Dictionary<string, InvoiceResult>();

    public SubmissionStatus Status => _status;

    /// <summary>Per-invoice results keyed by EIS unique ID (see InvoiceValidationService for the fallback keys).</summary>
    public IReadOnlyDictionary<string, InvoiceResult> Results => Volatile.Read(ref _results);

    public void Complete(IReadOnlyDictionary<string, InvoiceResult> results)
    {
        // Results are published before the status so a reader that sees COMPLETED also sees them
        Volatile.Write(ref _results, results);
        _status = SubmissionStatus.COMPLETED;
    }
}

public class SubmissionStore
{
    private readonly ConcurrentDictionary<string, Submission> submissions = new();

    // Unique IDs of invoices that were accepted, so later resends are rejected as duplicates
    private readonly ConcurrentDictionary<string, byte> acceptedUniqueIds = new();

    public bool TryAdd(Submission submission) => submissions.TryAdd(submission.SubmitId, submission);

    public bool Contains(string submitId) => submissions.ContainsKey(submitId);

    public bool TryGet(string submitId, [MaybeNullWhen(false)] out Submission submission) =>
        submissions.TryGetValue(submitId, out submission);

    public bool IsUniqueIdAccepted(string uniqueId) => acceptedUniqueIds.ContainsKey(uniqueId);

    /// <summary>Returns false if the unique ID was already accepted.</summary>
    public bool TryMarkUniqueIdAccepted(string uniqueId) => acceptedUniqueIds.TryAdd(uniqueId, 0);
}
