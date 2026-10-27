using Microsoft.AspNetCore.Mvc;

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