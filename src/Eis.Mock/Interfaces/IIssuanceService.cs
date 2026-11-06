using Eis.Mock.Auth;

namespace Eis.Mock.Interfaces;

/// <summary>The parts of the HTTP request the issuance checks need; Body is the raw request body.</summary>
public sealed record IssuanceRequest(
    string? Authorization,
    string? Signature,
    string? Datetime,
    string Method,
    string Path,
    string Body);

/// <summary>Either Data (the encrypted receipt) or Error is set. SubmitId is echoed in error bodies when readable.</summary>
public sealed record IssuanceResult(int StatusCode, string? Data, ErrorResponse? Error, string? SubmitId);

public interface IIssuanceService
{
    IssuanceResult Submit(IssuanceRequest request);
}
