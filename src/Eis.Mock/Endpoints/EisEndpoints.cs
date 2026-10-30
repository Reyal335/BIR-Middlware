using Microsoft.AspNetCore.Mvc;
using Eis.Mock.Registry;

namespace Eis.Mock.Endpoints;

[ApiController]
[Route("api/v2")]
public class EisController : Controller
{
    [Route("authentication")]
    [HttpPost]
    public IActionResult AuthenticateSystem(
        [FromHeader(Name = "accreditationId")] string accreditationId,
        [FromHeader(Name = "applicationId")] string applicationId
    )
    {
    if (!AppRegistry.Apps.TryGetValue(applicationId, out var app)
        || app.AccreditationId != accreditationId
        || !app.IsActive)
    {
        // return auth error
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