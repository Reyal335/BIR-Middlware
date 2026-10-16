using POS.Simulator.@class;
using POS.Simulator.Data;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Threading;
using Microsoft.Extensions.Http;

namespace POS.Simulator;


public class Worker(ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            await Task.Delay(1000, stoppingToken);
        }
    }
}

public class HttpPostingBackgroundService : BackgroundService
{
    private readonly ILogger<HttpPostingBackgroundService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public HttpPostingBackgroundService(IHttpClientFactory httpClientFactory, ILogger<HttpPostingBackgroundService> logger)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Time hosted service is running");
        
        await DoWork(_httpClientFactory, stoppingToken);

        using PeriodicTimer timer = new(TimeSpan.FromSeconds(2));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await DoWork(_httpClientFactory, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Time Hosted Service is stopping");
        }
    }

    private async Task DoWork(IHttpClientFactory _httpClientFactory, CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(2));

        POSInvoice Invoice = CreateSalesService.CreateOrders();
        
        using StringContent jsonContent = new (
            JsonSerializer.Serialize(Invoice), Encoding.UTF8, "application/json"
        );
        string readable = JsonSerializer.Serialize(Invoice, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        // Console.WriteLine(readable);
        var client = _httpClientFactory.CreateClient("MyApiService");

        var response = await client.PostAsync("Invoice", jsonContent, stoppingToken);
        var responseBody = await response.Content.ReadAsStringAsync(stoppingToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Invoice POST failed with status {StatusCode}: {ResponseBody}",
                (int)response.StatusCode,
                responseBody);
        }
        else
        {
            _logger.LogInformation("Invoice POST succeeded with status {StatusCode}", (int)response.StatusCode);
        }

        _logger.LogInformation("Time Hosted Service is working");
    }
}