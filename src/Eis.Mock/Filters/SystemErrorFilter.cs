using Eis.Mock.Invoices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Eis.Mock.Filters;

/// <summary>Turns an unhandled exception into 500 + SYS001 instead of leaking a stack trace.</summary>
public class SystemErrorFilter(ILogger<SystemErrorFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        logger.LogError(context.Exception, "Unhandled error on {Path}", context.HttpContext.Request.Path);

        context.Result = new ObjectResult(new
        {
            error_code = InvoiceErrorCodes.InternalError,
            message = "Internal error.",
            submitId = (string?)null,
        })
        {
            StatusCode = StatusCodes.Status500InternalServerError,
        };
        context.ExceptionHandled = true;
    }
}
