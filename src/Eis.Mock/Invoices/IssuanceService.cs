using System.Security.Cryptography;
using System.Text.Json;
using Eis.Mock.Auth;
using Eis.Mock.Dto;
using Eis.Mock.Interfaces;
using Eis.Mock.Store;

namespace Eis.Mock.Invoices;

public class IssuanceService(
    IssuanceRequestValidator validator,
    SubmissionStore submissionStore,
    InvoiceValidationQueue validationQueue) : IIssuanceService
{
    public IssuanceResult Submit(IssuanceRequest request)
    {
        var failure = validator.Validate(request, out var validated);
        if (failure is not null)
            return failure;

        DateTime receivedAt = ManilaClock.Now();

        var submission = new Submission
        {
            SubmitId = validated!.SubmitId,
            ReferenceNo = NewReferenceNo(receivedAt),
            ReceivedAt = receivedAt,
            TransmittedAt = validated.TransmittedAt,
            InvoiceCount = validated.Jws.Count,
            Jws = validated.Jws,
        };

        // Covers two concurrent requests with the same submitId that both passed the duplicate check
        if (!submissionStore.TryAdd(submission))
            return IssuanceRequestValidator.Fail(409, InvoiceErrorCodes.DuplicateSubmitId, "Duplicate submitId.", submission.SubmitId);

        var receipt = new IssuanceReceipt
        {
            submitId = submission.SubmitId,
            referenceNo = submission.ReferenceNo,
            receivedAt = $"{receivedAt:yyyy-MM-ddTHH:mm:ss}",
            invoiceCount = submission.InvoiceCount,
            status = submission.Status.ToString(),
        };

        string data = Aes256GcmHelper.Encrypt(JsonSerializer.Serialize(receipt), validated.SessionKey);

        // Per-invoice validation happens off the request path, in InvoiceValidationWorker
        validationQueue.Enqueue(submission.SubmitId);

        return new IssuanceResult(200, data, null, submission.SubmitId);
    }

    private static string NewReferenceNo(DateTime receivedAt) =>
        $"EIS{receivedAt:yyyyMMddHHmmss}{RandomNumberGenerator.GetHexString(6)}";
}
