// using POS.Simulator;

// var builder = Host.CreateApplicationBuilder(args);
// builder.Services.AddHostedService<Worker>();

// var host = builder.Build();
// host.Run();

using System.Text.Json;
using POS.Simulator.Data;

var invoice = CreateSalesService.CreateOrders().First();

var options = new JsonSerializerOptions
{
    WriteIndented = true
};

Console.WriteLine(JsonSerializer.Serialize(invoice, options));