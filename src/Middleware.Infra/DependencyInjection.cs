using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using Middleware.Infra.Data;
using Middleware.Infra.Eis;
using Middleware.Infra.Cache;
using Middleware.Infra.HMAC;
using Middleware.Infra.Time;
using Middleware.Infra.Repositories;

using Middleware.Core.interfaces;
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
        services.AddScoped<ITransmissionRepository, EfTransmissionRepository>();
        services.AddScoped<IEisClient, HttpEisClient>();
        services.AddScoped<IEisControlSequence, EisControlSequence>();
        services.AddScoped<TransmissionRun>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        services.AddSingleton<TaxEngine>();
        services.AddSingleton<EisMapper>();

        // Singleton: the key is loaded once and the RSA instance is disposed with the container
        services.AddSingleton<IEisSigner, EisSigner>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<EisTokenCache>();

        services.AddOptions<EisOptions>().Bind(configuration.GetSection("Eis")).ValidateOnStart();
        services.AddOptions<EisIdentityOptions>().Bind(configuration.GetSection("Eis")).ValidateOnStart();
        services.AddOptions<TransmissionWorkerOptions>().Bind(configuration.GetSection("TransmissionWorker")).ValidateOnStart();
        services.AddOptions<ResultPollerOptions>().Bind(configuration.GetSection("ResultPoller")).ValidateOnStart();
        services.AddOptions<ComplianceOptions>().Bind(configuration.GetSection("Compliance")).ValidateOnStart();
        services.AddOptions<TimeZoneOptions>().Bind(configuration.GetSection("TimeZone")).ValidateOnStart();

        services.AddScoped(sp => new EisUniqueIdGenerator(
            sp.GetRequiredService<IOptions<EisOptions>>().Value.CertificationNumber!,
            sp.GetRequiredService<IEisControlSequence>()
        ));

        services.AddHttpClient("Eis", (sp, client) =>
        {
            var eis = sp.GetRequiredService<IOptions<EisOptions>>();
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<EisOptions>>().Value.BaseUrl ?? "http://localhost:8000");
            client.Timeout = eis.Value.HttpTimeout;
        }).AddHttpMessageHandler(sp =>
        {
            // Main EIS calls are signed with the SessionKey from the current auth session
            var opts = sp.GetRequiredService<IOptions<EisOptions>>();
            var tokenCache = sp.GetRequiredService<EisTokenCache>();
            return new HmacSigningHandler(opts,
                async ct => (await tokenCache.GetEisSessionAsync(ct)).SessionKey);
        });

        services.AddHttpClient("EisAuth", (sp, client) => {
            client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<EisOptions>>().Value.BaseUrl ?? "http://localhost:8000");
        }).AddHttpMessageHandler(sp =>
        {
            // Auth calls are signed with the static ApplicationSecretKey.
            // Must NOT use EisTokenCache here: the cache calls this client, so it would deadlock.
            var opts = sp.GetRequiredService<IOptions<EisOptions>>();
            return new HmacSigningHandler(opts,
                _ => ValueTask.FromResult(opts.Value.ApplicationSecretKey
                    ?? throw new InvalidOperationException("Eis:ApplicationSecretKey is not configured")));
        });

        

        return services;
    }
}