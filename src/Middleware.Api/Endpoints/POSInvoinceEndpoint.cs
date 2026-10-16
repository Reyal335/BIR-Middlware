using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Middleware.Core.DTO;
using Middleware.Core.Models;
using Middleware.Core.Services;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Middleware.Api.Endpoints;

public class RandomWordDto
{
    public string RandomWord { get; set; } = string.Empty;
}

[ApiController]
[Route("[controller]")]
public class InvoiceController(InvoiceIntakeService intakeService, ILogger<InvoiceController> logger) : ControllerBase
{

    [HttpPost]
    public async Task<IActionResult> PostInvoice([FromBody] POSInvoice invoice_r)
    {
        logger.LogInformation("Heelllo its running");

        string readable = JsonSerializer.Serialize(invoice_r, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(readable);
        Invoice invoice = Invoice.FromPos(invoice_r);

        // Save Invoice to DB
        await intakeService.ReceiveInvocie(invoice);

        return Ok(new { message = $"Here is your random word"});
    }

}