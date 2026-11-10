using Eis.Mock.Interfaces;
using Eis.Mock.Auth;
using Eis.Mock.Invoices;
using Eis.Mock.Store;
using Eis.Mock.Workers;
using Microsoft.Extensions.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddTransient<ITokenIssuer, TokenIssuer>();
builder.Services.AddTransient<IAuthService, AuthService>();
builder.Services.AddSingleton<SessionStore>();

// Invoice issuance
builder.Services.AddSingleton<SubmissionStore>();
builder.Services.AddSingleton<InvoiceValidationQueue>();
builder.Services.AddSingleton<JwsVerifier>();
builder.Services.AddSingleton<InvoiceValidationService>();
builder.Services.AddTransient<RequestAuthenticator>();
builder.Services.AddTransient<IssuanceRequestValidator>();
builder.Services.AddTransient<IIssuanceService, IssuanceService>();
builder.Services.AddTransient<IInquiryService, InquiryService>();
builder.Services.AddHostedService<InvoiceValidationWorker>();

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
