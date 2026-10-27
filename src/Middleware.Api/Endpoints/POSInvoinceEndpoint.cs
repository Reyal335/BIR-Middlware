using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Middleware.Core.DTO;
using Middleware.Core.Models;
using Middleware.Core.Services;
using Middleware.Core.Tax;
using Middleware.Core.Eis;
using Middleware.Core.interfaces;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Middleware.Api.Endpoints;


[ApiController]
[Route("[controller]")]
public class InvoiceController(
    InvoiceIntakeService intakeService,
    ILogger<InvoiceController> logger,
    TaxEngine taxEngine,
    EisUniqueIdGenerator idGenerator) : ControllerBase

{

    [HttpGet] 
    public IActionResult TestConnection()
    {
        return Ok (new { message = "Server is up and running"});
    }

    [HttpPost]
    public async Task<IActionResult> PostInvoice([FromBody] POSInvoice invoice_r)
    {
        logger.LogInformation("Heelllo its running");

        // string readable = JsonSerializer.Serialize(invoice_r, new JsonSerializerOptions
        // {
        //     WriteIndented = true
        // });
        // Console.WriteLine(readable);
        var taxComputationResult = taxEngine.Compute(invoice_r);
        if (!taxComputationResult.IsSuccess) return UnprocessableEntity(new { Message = "Something went wrong with the calculation" });
        
        string eisId = await idGenerator.generateEisID();

        var invoice = Invoice.FromPos(invoice_r, taxComputationResult, eisId);
        

        // Queue invoice for EISTransmission worker to build payload
        invoice.MarkQueued();

        // Save Invoice to DB
        await intakeService.ReceiveInvocie(invoice);
        

        return Ok(new { message = $"Here is your random word"});
    }

}