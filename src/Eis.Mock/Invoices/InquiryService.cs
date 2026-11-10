using System.Text.Json;
using Eis.Mock.Auth;
using Eis.Mock.Dto;
using Eis.Mock.Interfaces;
using Eis.Mock.Store;

namespace Eis.Mock.Invoices;

/// <summary>
/// GET /api/v2/invoice_result/{submitId}: per-invoice results of a submission.
/// Any valid session may read any submission, since tokens rotate every 6 hours.
/// </summary>
public class InquiryService(RequestAuthenticator authenticator, SubmissionStore submissionStore) : IInquiryService
{
    public InquiryResult GetResults(InquiryRequest request)
    {
        // 1-4. Token, expiry, datetime window, signature (the submitId is in the signed path)
        var authFailure = authenticator.Authenticate(
            request.Authorization, request.Signature, request.Datetime, request.Method, request.Path,
            out string sessionKey, out _);
        if (authFailure is not null)
            return new InquiryResult(authFailure.StatusCode, null, authFailure.Error, request.SubmitId);

        if (!IssuanceRequestValidator.IsValidSubmitId(request.SubmitId))
            return Fail(400, InvoiceErrorCodes.InvalidSubmitId, "Invalid submitId format.", request.SubmitId);

        if (!submissionStore.TryGet(request.SubmitId, out var submission))
            return Fail(404, InvoiceErrorCodes.SubmitIdNotFound, "No submission with this submitId.", request.SubmitId);

        // Status is read before Results: Submission.Complete publishes the results first,
        // so seeing COMPLETED guarantees the results are visible
        SubmissionStatus status = submission.Status;
        var reply = new InquiryReply
        {
            submitId = submission.SubmitId,
            referenceNo = submission.ReferenceNo,
            status = status.ToString(),
            results = status == SubmissionStatus.COMPLETED
                ? submission.Results.Values
                    .OrderBy(r => r.Position)
                    .Select(r => new InquiryInvoiceResult
                    {
                        position = r.Position,
                        uniqueId = r.UniqueId,
                        code = r.Code,
                        message = r.Message,
                    })
                    .ToList()
                : [],
        };

        string data = Aes256GcmHelper.Encrypt(JsonSerializer.Serialize(reply), sessionKey);
        return new InquiryResult(200, data, null, submission.SubmitId);
    }

    private static InquiryResult Fail(int statusCode, string code, string message, string submitId) =>
        new(statusCode, null, new ErrorResponse(code, message), submitId);
}
