using System.Net;
using System.Text.Json;
using Eis.Mock.Interfaces;
using Eis.Mock.Invoices;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using static Eis.Mock.Tests.IssuanceKit;

namespace Eis.Mock.Tests;

/// <summary>Each test breaks exactly one transport check on an otherwise valid request.</summary>
public class IssuanceTransportTests(EisMockFactory factory) : IClassFixture<EisMockFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    private string ValidData(string sessionKey) =>
        EncryptInvoices([Sign(factory.Signer, ValidInvoice())], sessionKey);

    [Fact]
    public async Task Missing_bearer_token_returns_401_AUT001()
    {
        var session = AddSession(factory.Sessions);
        string submitId = NewSubmitId();

        var response = await client.SendAsync(Request(Body(submitId, ValidData(session.SessionKey)), token: null, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.MissingToken, submitId);
    }

    [Fact]
    public async Task Unknown_token_returns_401_AUT002()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), ValidData(session.SessionKey)), "UNKNOWNTOKEN0000000000000000000", session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.InvalidToken);
    }

    [Fact]
    public async Task Expired_token_returns_401_AUT002()
    {
        var session = AddSession(factory.Sessions, validFor: TimeSpan.FromMinutes(-1));

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), ValidData(session.SessionKey)), session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.InvalidToken);
    }

    [Theory]
    [InlineData(-11)]
    [InlineData(11)]
    public async Task Datetime_outside_10_minute_window_returns_401_AUT003(int offsetMinutes)
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), ValidData(session.SessionKey)), session.Token, session.SessionKey,
            datetime: ManilaNow.AddMinutes(offsetMinutes)));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.DatetimeOutOfWindow);
    }

    [Fact]
    public async Task Datetime_within_window_is_accepted()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), ValidData(session.SessionKey)), session.Token, session.SessionKey,
            datetime: ManilaNow.AddMinutes(-9)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Signature_made_with_another_key_returns_401_AUT004()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), ValidData(session.SessionKey)), session.Token, signingKey: NewSessionKey()));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.InvalidSignature);
    }

    [Fact]
    public async Task Missing_signature_returns_401_AUT004()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), ValidData(session.SessionKey)), session.Token, signingKey: null));

        await AssertErrorAsync(response, HttpStatusCode.Unauthorized, InvoiceErrorCodes.InvalidSignature);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"submitId\":\"ABCD1234-20261106120000-7F3A\"}")]
    [InlineData("{\"data\":\"AAAA\"}")]
    [InlineData("{\"submitId\":42,\"data\":\"AAAA\"}")]
    public async Task Malformed_body_returns_400_REQ001(string body)
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(body, session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.MalformedBody);
    }

    [Theory]
    [InlineData("not-a-submit-id")]
    [InlineData("ABCD1234-20261106120000-7f3a")]  // lowercase hex
    [InlineData("ABCD1234-20261399120000-7F3A")]  // month 13
    [InlineData("ABC1234-20261106120000-7F3A")]   // 7-char certification number
    public async Task Invalid_submit_id_returns_400_REQ002(string submitId)
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(Body(submitId, ValidData(session.SessionKey)), session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.InvalidSubmitId, submitId);
    }

    [Fact]
    public async Task Duplicate_submit_id_returns_409_REQ003()
    {
        var session = AddSession(factory.Sessions);
        string submitId = NewSubmitId();

        var first = await SubmitAsync(client, session, submitId, [Sign(factory.Signer, ValidInvoice())]);
        var second = await SubmitAsync(client, session, submitId, [Sign(factory.Signer, ValidInvoice())]);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        await AssertErrorAsync(second, HttpStatusCode.Conflict, InvoiceErrorCodes.DuplicateSubmitId, submitId);
    }

    [Fact]
    public async Task Data_encrypted_with_another_key_returns_400_REQ004()
    {
        var session = AddSession(factory.Sessions);
        string submitId = NewSubmitId();

        var response = await client.SendAsync(Request(Body(submitId, ValidData(NewSessionKey())), session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.DecryptionFailed, submitId);
    }

    [Fact]
    public async Task Data_that_is_not_base64_returns_400_REQ004()
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(Body(NewSubmitId(), "%%% not base64 %%%"), session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.DecryptionFailed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Invoice_count_outside_1_to_100_returns_400_REQ005(int count)
    {
        var session = AddSession(factory.Sessions);
        string jws = Sign(factory.Signer, ValidInvoice());

        var response = await SubmitAsync(client, session, NewSubmitId(), Enumerable.Repeat(jws, count));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.InvalidInvoiceCount);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{}")]
    [InlineData("{\"invoices\":[1,2]}")]
    public async Task Decrypted_payload_without_a_jws_array_returns_400_REQ005(string plainText)
    {
        var session = AddSession(factory.Sessions);

        var response = await client.SendAsync(Request(
            Body(NewSubmitId(), Encrypt(plainText, session.SessionKey)), session.Token, session.SessionKey));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, InvoiceErrorCodes.InvalidInvoiceCount);
    }

    [Fact]
    public async Task One_hundred_invoices_are_accepted()
    {
        var session = AddSession(factory.Sessions);
        string jws = Sign(factory.Signer, ValidInvoice());

        var response = await SubmitAsync(client, session, NewSubmitId(), Enumerable.Repeat(jws, 100));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unexpected_exception_returns_500_SYS001_without_details()
    {
        using var throwing = factory.WithWebHostBuilder(b => b.ConfigureTestServices(services =>
            services.AddTransient<IIssuanceService, ThrowingIssuanceService>()));
        var session = AddSession(throwing.Services.GetRequiredService<Store.SessionStore>());

        var response = await SubmitAsync(throwing.CreateClient(), session, NewSubmitId(), [Sign(factory.Signer, ValidInvoice())]);

        await AssertErrorAsync(response, HttpStatusCode.InternalServerError, InvoiceErrorCodes.InternalError);
        string body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("secret failure detail", body);
    }

    private sealed class ThrowingIssuanceService : IIssuanceService
    {
        public IssuanceResult Submit(IssuanceRequest request) =>
            throw new InvalidOperationException("secret failure detail");
    }
}
