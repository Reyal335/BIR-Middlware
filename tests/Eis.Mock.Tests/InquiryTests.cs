using System.Net;
using System.Text.Json;
using Eis.Mock.Invoices;
using Eis.Mock.Store;
using static Eis.Mock.Tests.IssuanceKit;

namespace Eis.Mock.Tests;

/// <summary>GET /api/v2/invoice_result/{submitId}</summary>
public class InquiryTests(EisMockFactory factory) : IClassFixture<EisMockFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Missing_bearer_token_returns_401_AUT001()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(InquiryRequest(NewSubmitId(), token: null, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.MissingToken);
    }

    [Fact]
    public async Task Expired_token_returns_401_AUT002()
    {
        var session = AddSession(factory.Sessions, validFor: TimeSpan.FromMinutes(-1));

        var response = await client.SendAsync(InquiryRequest(NewSubmitId(), session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.InvalidToken);
    }

    [Fact]
    public async Task Datetime_outside_window_returns_401_AUT003()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(InquiryRequest(
            NewSubmitId(), session.Token, session.SessionKey, datetime: ManilaNow.AddMinutes(-11)));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.DatetimeOutOfWindow);
    }

    [Fact]
    public async Task Signature_over_another_submit_id_returns_401_AUT004()
    {
        // The submitId is part of the signed path, so a signature for one ID can't be reused for another
        var session = AddSession(factory.Sessions);
        string signedFor = NewSubmitId();
        string requested = NewSubmitId();
        var request = InquiryRequest(signedFor, session.Token, session.SessionKey);
        request.RequestUri = new Uri($"{InquiryPath}/{requested}", UriKind.Relative);

        var response = await client.SendAsync(request);

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.InvalidSignature, requested);
    }

    [Fact]
    public async Task Invalid_submit_id_returns_400_REQ002()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(InquiryRequest("not-a-submit-id", session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.InvalidSubmitId, "not-a-submit-id");
    }

    [Fact]
    public async Task Unknown_submit_id_returns_404_REQ006()
    {
        var session = AddSession(factory.Sessions);
        string submitId = NewSubmitId();

        var response = await client.SendAsync(InquiryRequest(submitId, session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.NotFound, InvoiceErrorCodes.SubmitIdNotFound, submitId);
    }

    [Fact]
    public async Task Batch_still_being_validated_returns_RECEIVED_with_no_results()
    {
        // Stored directly, without queueing validation, so it stays RECEIVED
        var session = AddSession(factory.Sessions);
        string submitId = NewSubmitId();
        factory.Submissions.TryAdd(new Submission
        {
            SubmitId = submitId,
            ReferenceNo = "REF-RECEIVED",
            ReceivedAt = ManilaNow.DateTime,
            TransmittedAt = ManilaNow.DateTime,
            InvoiceCount = 1,
            Jws = ["x.y.z"],
        });

        var reply = await GetReplyAsync(submitId, session);

        Assert.Equal(submitId, reply.GetProperty("submitId").GetString());
        Assert.Equal("REF-RECEIVED", reply.GetProperty("referenceNo").GetString());
        Assert.Equal("RECEIVED", reply.GetProperty("status").GetString());
        Assert.Equal(0, reply.GetProperty("results").GetArrayLength());
    }

    [Fact]
    public async Task Completed_batch_returns_every_result_in_position_order()
    {
        var session = AddSession(factory.Sessions);
        var accepted = ValidInvoice();
        var unregistered = ValidInvoice();
        unregistered.Seller.Tin = "999999999";
        string submitId = NewSubmitId();

        var submit = await SubmitAsync(client, session, submitId,
            [Sign(factory.Signer, accepted), Sign(factory.Signer, unregistered)]);
        Assert.Equal(HttpStatusCode.OK, submit.StatusCode);
        var stored = await WaitForCompletionAsync(factory.Submissions, submitId);

        // A different session (e.g. after token refresh) can read it too
        var reply = await GetReplyAsync(submitId, AddSession(factory.Sessions));

        Assert.Equal("COMPLETED", reply.GetProperty("status").GetString());
        Assert.Equal(stored.ReferenceNo, reply.GetProperty("referenceNo").GetString());

        var results = reply.GetProperty("results").EnumerateArray().ToList();
        Assert.Equal(2, results.Count);

        Assert.Equal(1, results[0].GetProperty("position").GetInt32());
        Assert.Equal(accepted.EisUniqueId, results[0].GetProperty("uniqueId").GetString());
        Assert.Equal(InvoiceResultCodes.Accepted, results[0].GetProperty("code").GetString());

        Assert.Equal(2, results[1].GetProperty("position").GetInt32());
        Assert.Equal(unregistered.EisUniqueId, results[1].GetProperty("uniqueId").GetString());
        Assert.Equal(InvoiceResultCodes.UnregisteredTin, results[1].GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(results[1].GetProperty("message").GetString()));
    }

    /// <summary>Sends a valid inquiry and returns the decrypted reply.</summary>
    private async Task<JsonElement> GetReplyAsync(string submitId, (string Token, string SessionKey) session)
    {
        var response = await client.SendAsync(InquiryRequest(submitId, session.Token, session.SessionKey));
        string json = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.OK, $"Expected 200, got {(int)response.StatusCode}: {json}");

        // Single-field body, encrypted with the session key of the request
        using var body = JsonDocument.Parse(json);
        Assert.Equal(["data"], body.RootElement.EnumerateObject().Select(p => p.Name));
        string plain = Decrypt(body.RootElement.GetProperty("data").GetString()!, session.SessionKey);

        return JsonDocument.Parse(plain).RootElement.Clone();
    }
}
