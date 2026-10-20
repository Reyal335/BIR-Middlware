using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Middleware.Infra.Data;
using Middleware.Infra.Eis;

using Middleware.Core.interfaces;
using Middleware.Core.Repositories;
using Middleware.Core.Services;
using Middleware.Core.Tax;
using Middleware.Core.Eis;
using Microsoft.Extensions.Options;

namespace Middleware.Infra;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database connection string
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        services.AddSqlite<AppDb>(connectionString);

        services.AddScoped<InvoiceIntakeService>();
        services.AddScoped<IInvoiceRepository, EFInvoiceRepository>();
        services.AddScoped<IEisClient, HttpEisClient>();
        services.AddScoped<IEisControlSequence, EisControlSequence>();
        services.AddSingleton<TaxEngine>();

        services.AddOptions<EisOptions>().Bind(configuration.GetSection("Eis")).ValidateOnStart();
        services.AddOptions<TransmissionWorkerOptions>().Bind(configuration.GetSection("TransmissionWorker")).ValidateOnStart();
        services.AddOptions<ResultPollerOptions>().Bind(configuration.GetSection("ResultPoller")).ValidateOnStart();
        services.AddOptions<ComplianceOptions>().Bind(configuration.GetSection("Compliance")).ValidateOnStart();

        services.AddScoped(sp => new EisUniqueIdGenerator(
            sp.GetRequiredService<IOptions<EisOptions>>().Value.CertificationNumber!,
            sp.GetRequiredService<IEisControlSequence>()
        ));

        return services;
    }
}