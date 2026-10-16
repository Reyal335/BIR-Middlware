using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Middleware.Infra.Data;
using Middleware.Core.interfaces;
using Middleware.Core.Repositories;
using Middleware.Core.Services;


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
        return services;
    }
}