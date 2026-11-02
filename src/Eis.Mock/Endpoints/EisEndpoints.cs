using Microsoft.AspNetCore.Mvc;
using Eis.Mock.Registry;
using Eis.Mock.Auth;
using Eis.Mock.DTO;
using System;
using System.Globalization;

namespace Eis.Mock.Endpoints;

[ApiController]
[Route("api/v2")]
public class EisController : Controller
{
    [Route("authentication")]
    [HttpPost]
    public IActionResult AuthenticateSystem(
        [FromHeader(Name = "accreditationId")] string accreditationId,
        [FromHeader(Name = "applicationId")] string applicationId,
        [FromHeader(Name = "datetime")] string datetime
    )

    // If there is no App registered on linked appliationId return an error
    {
        DateTime referenceTime = DateTime.UtcNow;

        if (!AppRegistry.Apps.TryGetValue(applicationId, out var app)
            || app.AccreditationId != accreditationId
            || !app.IsActive)
        {
            // return auth error
            var error = AuthValidator.Validate(applicationId, accreditationId);
            return Unauthorized(new {error_code = error!.Code, message = error!.Message});
        }

        if (!DateTime.TryParseExact(datetime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime result))
        {
            return Unauthorized(new
            {
                error_code = AuthErrorCodes.InvalidDateTime,
                message = "Invalid datetime"
            });
        }

        if (Math.Abs((result - referenceTime).TotalMinutes) < 10)
        {
            return Unauthorized(new
            {
                error_code = AuthErrorCodes.ExceedElapsedTime,
                message = "Exceeded the elapsed time of 10 minutes"
            });
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