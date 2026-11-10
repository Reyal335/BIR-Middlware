using Eis.Mock.Invoices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Middleware.Core.DTO;
using Middleware.Core.Eis;
using Middleware.Core.interfaces;
using Middleware.Core.Models;
using Middleware.Core.Tax;
using Middleware.Infra;
using Middleware.Infra.Eis;
using static Eis.Mock.Tests.IssuanceKit;

namespace Eis.Mock.Tests;

/// <summary>
/// Runs the middleware's own code end to end against the in-memory mock: AddInfrastructure wiring,
/// TaxEngine, Invoice, EisMapper, EisJson, EisSigner, EisTokenCache (real authentication),
/// HmacSigningHandler, HttpEisClient.SubmitBatch and HttpEisClient.GetResults.
/// </summary>
public class MiddlewareContractTests(EisMockFactory factory) : IClassFixture<EisMockFactory>
{
    [Fact]
    public async Task Middleware_batch_is_received_and_every_invoice_accepted()
    {
        using var provider = BuildMiddleware();
        var taxEngine = provider.GetRequiredService<TaxEngine>();
        var mapper = provider.GetRequiredService<EisMapper>();
        var signer = provider.GetRequiredService<IEisSigner>();

        DateTimeOffset issuedAt = ManilaNow.AddMinutes(-5);

        // Walk-in: VATABLE + ZERO_RATED
        var walkIn = PosInvoice("INV-1", issuedAt, "WALK_IN", 162.00,
            Line(1, "SKU-1", 2, 56.00, vatable: true, null),
            Line(2, "SKU-2", 1, 50.00, vatable: false, taxType.ZERO_RATED));

        // Senior citizen: VATABLE + VAT_EXEMPT_SCPWD (112 -> 100.00 exempt - 20.00 discount = 80.00)
        var senior = PosInvoice("INV-2", issuedAt, "SENIOR_CITIZEN", 192.00,
            Line(1, "SKU-3", 1, 112.00, vatable: true, null),
            Line(2, "SKU-4", 1, 112.00, vatable: false, taxType.VAT_EXEMPT_SCPWD));

        var invoices = new List<Invoice>();
        foreach (var dto in new[] { walkIn, senior })
        {
            var tax = taxEngine.Compute(dto);
            Assert.True(tax.IsSuccess, string.Join("; ", tax.Errors));

            // Same steps as InvoiceIntakeService + TransmissionRun
            var invoice = Invoice.FromPos(dto, tax, NewUniqueId(issuedAt));
            invoice.MarkQueued();
            invoice.AttachSignature(signer.Sign(EisJson.Serialize(mapper.Map(invoice))));
            invoices.Add(invoice);
        }

        string submitId = SubmitIdGenerator.Create(CertificationNumber, ManilaNow);

        EisReceipt receipt;
        using (var scope = provider.CreateScope())
        {
            // Throws on any non-2xx response; decrypts the receipt with the session key
            receipt = await scope.ServiceProvider.GetRequiredService<IEisClient>()
                .SubmitBatch(submitId, invoices, CancellationToken.None);
        }

        // The decrypted receipt matches what the mock stored for this submission
        Assert.True(factory.Submissions.TryGet(submitId, out var stored));
        Assert.Equal(submitId, receipt.SubmitId);
        Assert.Equal(stored.ReferenceNo, receipt.ReferenceNo);
        Assert.Equal(2, receipt.InvoiceCount);
        Assert.Equal("RECEIVED", receipt.Status);
        Assert.Equal($"{stored.ReceivedAt:yyyy-MM-ddTHH:mm:ss}", receipt.ReceivedAt);

        // Second round trip: the middleware asks for results until the batch is COMPLETED
        EisInquiryResult inquiry = await PollResultsAsync(provider, submitId);

        Assert.Equal(stored.ReferenceNo, inquiry.ReferenceNo);
        Assert.Equal(2, inquiry.Results.Count);
        foreach (var invoice in invoices)
        {
            var result = Assert.Single(inquiry.Results, r => r.UniqueId == invoice.EisUniqueId);
            Assert.True(result.Code == EisResultCodes.Accepted,
                $"{invoice.InvoiceNumber}: expected {EisResultCodes.Accepted}, got {result.Code} ({result.Message})");
        }
    }

    [Fact]
    public async Task Inquiry_for_an_unknown_submission_throws_EisSubmissionNotFoundException()
    {
        using var provider = BuildMiddleware();
        string submitId = SubmitIdGenerator.Create(CertificationNumber, ManilaNow);

        using var scope = provider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<IEisClient>();

        var ex = await Assert.ThrowsAsync<EisSubmissionNotFoundException>(
            () => client.GetResults(submitId, CancellationToken.None));
        Assert.Equal(submitId, ex.SubmitId);
    }

    private static async Task<EisInquiryResult> PollResultsAsync(ServiceProvider provider, string submitId)
    {
        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (true)
        {
            using var scope = provider.CreateScope();
            var result = await scope.ServiceProvider.GetRequiredService<IEisClient>()
                .GetResults(submitId, CancellationToken.None);

            Assert.Equal(submitId, result.SubmitId);
            if (result.Status == EisBatchStatus.Completed)
                return result;

            // Still validating: no results are returned yet
            Assert.Equal(EisBatchStatus.Received, result.Status);
            Assert.Empty(result.Results);

            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Submission {submitId} was not COMPLETED within 10 seconds.");
            await Task.Delay(25);
        }
    }

    private ServiceProvider BuildMiddleware()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",
            ["Eis:BaseUrl"] = "http://localhost",
            ["Eis:AuthPath"] = "/api/v2/authentication",
            ["Eis:SubmitPath"] = IssuanceKit.Path,
            ["Eis:ResultPath"] = InquiryPath,
            ["Eis:CertificationNumber"] = CertificationNumber,
            ["Eis:AccreditationId"] = "ACC-TEST-001",
            ["Eis:ApplicationId"] = "APP-GAP",
            ["Eis:HttpTimeout"] = "00:00:30",
            ["Eis:UserId"] = "mockuser",
            ["Eis:Password"] = "Test123",
            ["Eis:ApplicationSecretKey"] = "test-application-secret",
            ["Eis:BirPublicKey"] = factory.AuthPublicKey,
            ["Eis:SigningKey"] = factory.SigningPrivateKey,
        }).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        // Route both named clients into the in-memory mock; their signing handlers stay as configured
        services.AddHttpClient("Eis").ConfigurePrimaryHttpMessageHandler(() => factory.Server.CreateHandler());
        services.AddHttpClient("EisAuth").ConfigurePrimaryHttpMessageHandler(() => factory.Server.CreateHandler());

        return services.BuildServiceProvider();
    }

    private static POSInvoice PosInvoice(
        string number, DateTimeOffset issuedAt, string customerType, double totalDue, params PosLineItems[] lines) => new()
    {
        PosTerminalId = "POS-01",
        StoreCode = "STORE-01",
        TransactionId = Guid.NewGuid().ToString("N"),
        InvoiceType = "SALES_INVOICE",
        InvoiceNumber = number,
        TransactionDateTime = issuedAt,
        Cashier = "cashier",
        Seller = new Seller
        {
            RegisteredName = "Test Seller Inc.",
            TradeName = "Test Seller",
            Tin = RegisteredTin,
            BranchCode = "00000",
            Address = "Makati City",
            VatRegistration = "VAT",
            PtuOrAccerditationNo = "123-456-7890-12345",
            MachineAccreditationNo = "123-45678901",
        },
        Customer = new Customer { Type = customerType },
        LineItems = [.. lines],
        Totals = new Totals { TotalDue = totalDue },
        Payment = new Payment { Method = "CASH", AmountTendered = totalDue, Change = 0 },
    };

    private static PosLineItems Line(int lineNo, string sku, int qty, double unitPrice, bool vatable, taxType? taxType) => new()
    {
        LineNo = lineNo,
        SKU = sku,
        Description = sku,
        Quantity = qty,
        UnitPrice = unitPrice,
        LineAmount = qty * unitPrice,
        Vatable = vatable,
        TaxType = taxType,
    };
}
