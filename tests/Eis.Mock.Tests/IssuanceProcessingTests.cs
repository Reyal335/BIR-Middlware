using System.Globalization;
using System.Net;
using System.Text.Json;
using Eis.Mock.Invoices;
using Eis.Mock.Store;
using Middleware.Core.Eis;
using static Eis.Mock.Tests.IssuanceKit;

namespace Eis.Mock.Tests;

public class IssuanceProcessingTests(EisMockFactory factory) : IClassFixture<EisMockFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Valid_batch_returns_an_encrypted_receipt_and_stores_the_batch()
    {
        var session = AddSession(factory.Sessions);
        string submitId = NewSubmitId();
        string[] jws = [Sign(factory.Signer, ValidInvoice()), Sign(factory.Signer, ValidInvoice())];
        DateTime before = ManilaNow.DateTime.AddSeconds(-1);

        var response = await SubmitAsync(client, session, submitId, jws);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Single-field body; the receipt is only readable with the session key
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["data"], body.RootElement.EnumerateObject().Select(p => p.Name));
        string data = body.RootElement.GetProperty("data").GetString()!;
        Assert.DoesNotContain(submitId, data);

        using var receipt = JsonDocument.Parse(Decrypt(data, session.SessionKey));
        var r = receipt.RootElement;
        Assert.Equal(submitId, r.GetProperty("submitId").GetString());
        Assert.Equal(2, r.GetProperty("invoiceCount").GetInt32());
        Assert.Equal("RECEIVED", r.GetProperty("status").GetString());
        Assert.False(string.IsNullOrWhiteSpace(r.GetProperty("referenceNo").GetString()));

        DateTime receivedAt = DateTime.ParseExact(r.GetProperty("receivedAt").GetString()!, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);
        Assert.InRange(receivedAt, before, ManilaNow.DateTime.AddSeconds(1));

        Assert.True(factory.Submissions.TryGet(submitId, out var stored));
        Assert.Equal(r.GetProperty("referenceNo").GetString(), stored.ReferenceNo);
        Assert.Equal(2, stored.InvoiceCount);
        Assert.Equal(jws, stored.Jws);
    }

    [Fact]
    public async Task Invoices_in_a_batch_get_their_own_result_codes()
    {
        var session = AddSession(factory.Sessions);

        // An earlier batch accepts invoice A, so resending it later is a duplicate
        var invoiceA = ValidInvoice();
        string firstSubmitId = NewSubmitId();
        var first = await SubmitAsync(client, session, firstSubmitId, [Sign(factory.Signer, invoiceA)]);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var firstBatch = await WaitForCompletionAsync(factory.Submissions, firstSubmitId);
        Assert.Equal(InvoiceResultCodes.Accepted, firstBatch.Results[invoiceA.EisUniqueId].Code);

        var untrusted = EisMockFactory.CreateUntrustedSigner();

        var badSignature = ValidInvoice();

        var schema = ValidInvoice();
        schema.Seller.Tin = null;

        var unregisteredTin = ValidInvoice();
        unregisteredTin.Seller.Tin = "999999999";

        var future = ManilaNow.AddHours(1);
        var issuedAfterTransmission = ValidInvoice(issuedAt: future);

        var dateMismatch = ValidInvoice(uniqueId: NewUniqueId(ManilaNow.AddDays(-3)));

        var wrongTotals = ValidInvoice();
        wrongTotals.TaxSummary.TotalAmountDue = 100.00m;

        var exemptWithVat = ValidInvoice();
        exemptWithVat.ItemList[0].TaxType = "VAT_EXEMPT";
        exemptWithVat.TaxSummary = new EisTaxSummary { VatExemptSales = 112.00m, VatAmount = 12.00m, TotalAmountDue = 112.00m };

        var zeroRated = ValidInvoice();
        zeroRated.ItemList[0].TaxType = "ZERO_RATED";
        zeroRated.TaxSummary = new EisTaxSummary { ZeroRatedSales = 112.00m, TotalAmountDue = 112.00m };

        var valid = ValidInvoice();

        string secondSubmitId = NewSubmitId();
        var second = await SubmitAsync(client, session, secondSubmitId,
        [
            Sign(factory.Signer, invoiceA),
            Sign(untrusted, badSignature),
            Sign(factory.Signer, schema),
            Sign(factory.Signer, unregisteredTin),
            Sign(factory.Signer, issuedAfterTransmission),
            Sign(factory.Signer, dateMismatch),
            Sign(factory.Signer, wrongTotals),
            Sign(factory.Signer, exemptWithVat),
            Sign(factory.Signer, zeroRated),
            Sign(factory.Signer, valid),
        ]);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);

        var batch = await WaitForCompletionAsync(factory.Submissions, secondSubmitId);
        var results = batch.Results;

        Assert.Equal(10, results.Count);
        AssertResult(results, invoiceA, 1, InvoiceResultCodes.DuplicateUniqueId);
        AssertResult(results, badSignature, 2, InvoiceResultCodes.SignatureInvalid);
        AssertResult(results, schema, 3, InvoiceResultCodes.SchemaInvalid);
        AssertResult(results, unregisteredTin, 4, InvoiceResultCodes.UnregisteredTin);
        AssertResult(results, issuedAfterTransmission, 5, InvoiceResultCodes.InvalidDatetime);
        AssertResult(results, dateMismatch, 6, InvoiceResultCodes.InvalidDatetime);
        AssertResult(results, wrongTotals, 7, InvoiceResultCodes.TotalsMismatch);
        AssertResult(results, exemptWithVat, 8, InvoiceResultCodes.TotalsMismatch);
        AssertResult(results, zeroRated, 9, InvoiceResultCodes.Accepted);
        AssertResult(results, valid, 10, InvoiceResultCodes.Accepted);

        Assert.Contains("VATAmount must be 0.00", results[exemptWithVat.EisUniqueId].Message);

        // Accepted IDs are recorded; rejected ones are not
        Assert.True(factory.Submissions.IsUniqueIdAccepted(valid.EisUniqueId));
        Assert.False(factory.Submissions.IsUniqueIdAccepted(wrongTotals.EisUniqueId));

        untrusted.Dispose();
    }

    [Fact]
    public async Task Same_unique_id_twice_in_one_batch_keeps_both_results()
    {
        var session = AddSession(factory.Sessions);
        var invoice = ValidInvoice();
        string jws = Sign(factory.Signer, invoice);
        string submitId = NewSubmitId();

        await SubmitAsync(client, session, submitId, [jws, jws]);
        var batch = await WaitForCompletionAsync(factory.Submissions, submitId);

        Assert.Equal(InvoiceResultCodes.Accepted, batch.Results[invoice.EisUniqueId].Code);
        Assert.Equal(InvoiceResultCodes.DuplicateUniqueId, batch.Results[$"{invoice.EisUniqueId}#2"].Code);
    }

    private static void AssertResult(
        IReadOnlyDictionary<string, InvoiceResult> results, BirEisInvoicePayload invoice, int position, string code)
    {
        Assert.True(results.TryGetValue(invoice.EisUniqueId, out var result), $"No result for {invoice.EisUniqueId}");
        Assert.True(code == result.Code, $"Invoice #{position}: expected {code}, got {result.Code} ({result.Message})");
        Assert.Equal(position, result.Position);
    }
}
