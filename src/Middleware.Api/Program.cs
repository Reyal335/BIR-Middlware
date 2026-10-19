using Middleware.Infra;
using Middleware.Infra.Data;
using Middleware.Core.Models;
using Middleware.Api.Workers;  

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();


builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<EisTransmissionWorker>();   

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

app.Run();

