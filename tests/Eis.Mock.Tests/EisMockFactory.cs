using System.Security.Cryptography;
using Eis.Mock.Store;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Middleware.Infra.Eis;

namespace Eis.Mock.Tests;

/// <summary>
/// Hosts the mock in memory with a fresh JWS key pair. The private half drives the middleware's
/// real EisSigner; the public half is given to the mock as Mock:SigningPublicKey.
/// </summary>
public sealed class EisMockFactory : WebApplicationFactory<Program>
{
    private readonly RSA signingKey = RSA.Create(2048);

    public EisSigner Signer { get; }

    public EisMockFactory()
    {
        Signer = CreateSigner(signingKey);
    }

    /// <summary>A signer whose key the mock does not trust.</summary>
    public static EisSigner CreateUntrustedSigner() => CreateSigner(RSA.Create(2048));

    public SessionStore Sessions => Services.GetRequiredService<SessionStore>();
    public SubmissionStore Submissions => Services.GetRequiredService<SubmissionStore>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not "Development", so the developer's user secrets are not loaded
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mock:SigningPublicKey"] = Convert.ToBase64String(signingKey.ExportSubjectPublicKeyInfo()),
        }));
    }

    private static EisSigner CreateSigner(RSA key) => new(Options.Create(new EisOptions
    {
        SigningKey = Convert.ToBase64String(key.ExportPkcs8PrivateKey()),
        SigningKeyId = "test-key",
    }));

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            Signer.Dispose();
            signingKey.Dispose();
        }
    }
}
