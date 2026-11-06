using Microsoft.AspNetCore.Mvc;
using Eis.Mock.DTO;
using Eis.Mock.Interfaces;
using Eis.Mock.Filters;

namespace Eis.Mock.Endpoints;

[ApiController]
[Route("api/v2")]
public class EisController : Controller
{
    private readonly IAuthService _authService;
    private readonly IIssuanceService _issuanceService;

    public EisController(IAuthService authService, IIssuanceService issuanceService)
    {
        _authService = authService;
        _issuanceService = issuanceService;
    }

    /** 
        Authentication Endpoint
    **/
    [Route("authentication")]
    [HttpPost]
    public IActionResult AuthenticateSystem(
        [FromHeader(Name = "accreditationId")] string accreditationId,
        [FromHeader(Name = "applicationId")] string applicationId,
        [FromHeader(Name = "datetime")] string datetime,
        [FromBody] AuthBody body
    )
    {
        var result = _authService.Authenticate(applicationId, accreditationId, datetime, body);
        
        if (result.Error is not null)
        {
            return Unauthorized(new {error_code = result.Error.Code, message = result.Error.Message});
        }

        return Ok(new { status = "200", data = result.data });
    }

    /**
        Invoice Issuance endpoint 
    **/
    [Route("invoices")]
    [HttpPost]
    [TypeFilter(typeof(SystemErrorFilter))]
    public async Task<IActionResult> InvoiceInsurance(CancellationToken ct)
    {
        // Raw body: the service reports malformed JSON with REQ001, after the auth checks
        using var reader = new StreamReader(Request.Body);
        string body = await reader.ReadToEndAsync(ct);

        var result = _issuanceService.Submit(new IssuanceRequest(
            Authorization: Request.Headers.Authorization,
            Signature: Request.Headers["signature"],
            Datetime: Request.Headers["datetime"],
            Method: Request.Method,
            Path: $"{Request.PathBase}{Request.Path}",
            Body: body));

        if (result.Error is not null)
        {
            return StatusCode(result.StatusCode,
                new { error_code = result.Error.Code, message = result.Error.Message, submitId = result.SubmitId });
        }

        return Ok(new { data = result.Data });
    }

    [Route("invoice_result/{submitId}")]
    [HttpGet]
    public IActionResult GetInvoiceResult()
    {
        return Ok(new {message = "nice"});
    }
}
