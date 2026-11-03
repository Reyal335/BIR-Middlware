using Microsoft.AspNetCore.Mvc;
using Eis.Mock.DTO;
using Eis.Mock.Interfaces;

namespace Eis.Mock.Endpoints;

[ApiController]
[Route("api/v2")]
public class EisController : Controller
{
    private readonly IAuthService _authService;

    public EisController(IAuthService authService)
    {
        _authService = authService;
    }

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

        return Ok(new {message = "nice"});
    }

    [Route("invoices")]
    [HttpPost]
    public IActionResult InvoiceInsurance()
    {
        return Ok(new {message = "nice"});
    }

    [Route("invoice_result/{submitId}")]
    [HttpGet]
    public IActionResult GetInvoiceResult()
    {
        return Ok(new {message = "nice"});
    }
}
