using Eis.Mock.Interfaces;
using Eis.Mock.Auth;
using Eis.Mock.Store;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddTransient<ITokenIssuer, TokenIssuer>();
builder.Services.AddSingleton<SessionStore>();


var app = builder.Build();

app.Run();
