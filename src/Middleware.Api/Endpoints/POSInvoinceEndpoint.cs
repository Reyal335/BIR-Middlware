using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Middleware.Core.DTO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Middleware.Api.Endpoints;

public class RandomWordDto
{
    public string RandomWord { get; set; } = string.Empty;
}

[ApiController]
[Route("[controller]")]
public class InvoiceController(ILogger<InvoiceController> logger) : ControllerBase
{

    [HttpGet]
    public IActionResult SayHello()
    {
        logger.LogInformation("Hello from the time hosted service!");
        return Ok("You got your server");
    }

    [HttpPost]
    public IActionResult PostInvoice([FromBody] POSInvoice invoice)
    {
        string readable = JsonSerializer.Serialize(invoice, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        Console.WriteLine(readable);
        return Ok(new { message = $"Here is your random word"});
    }

    [HttpPost("/random")]
    public IActionResult RandomWord([FromBody] RandomWordDto body)
    {
        string word = body.RandomWord;
        return Ok(new { message = $"Here is your random word {word}"});
    }
}