using POS.Simulator;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHttpClient("MyApiService", client =>
{
    client.BaseAddress = new Uri("http://localhost:5077");
});
builder.Services.AddHostedService<HttpPostingBackgroundService>();

var host = builder.Build();
host.Run();
