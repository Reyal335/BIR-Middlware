using Eis.Mock.Auth;

namespace Eis.Mock.Interfaces;

/// <summary>The parts of the HTTP request the inquiry needs; SubmitId comes from the route.</summary>
public sealed record InquiryRequest(
    string? Authorization,
    string? Signature,
    string? Datetime,
    string Method,
    string Path,
    string SubmitId);

/// <summary>Either Data (the encrypted reply) or Error is set.</summary>
public sealed record InquiryResult(int StatusCode, string? Data, ErrorResponse? Error, string? SubmitId);

public interface IInquiryService
{
    InquiryResult GetResults(InquiryRequest request);
}
