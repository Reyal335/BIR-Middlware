using Eis.Mock.Interfaces;
using Eis.Mock.Auth;    
using Eis.Mock.DTO;
using System;
using System.Text.Json;
using System.Globalization;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace Eis.Mock.Auth;

public class AuthService (ITokenIssuer tokenIssuer, IConfiguration configuration) : IAuthService
{
    public AuthResult Authenticate(string applicationId, string accreditationId, string datetime, AuthBody authBody)
    {
        DateTime referenceTime = DateTime.UtcNow;

        // If there is no App registered on linked appliationId return an error
        var error = AuthValidator.Validate(applicationId, accreditationId);
        if (error is not null)
            return new AuthResult(null, error);

        if (!DateTime.TryParseExact(datetime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out DateTime result))
        {
            return new AuthResult(null, new ErrorResponse(AuthErrorCodes.InvalidDateTime, "Invalid datetime"));
        }

        if (Math.Abs((result - referenceTime).TotalMinutes) > 10)
        {
            return new AuthResult(null, new ErrorResponse(AuthErrorCodes.ExceedElapsedTime, "Exceeded the elapsed time of 10 minutes"));
        }

        // Decrypt the request body
        string privateKey = configuration["Mock:EisPrivateKey"]
            ?? throw new InvalidOperationException("Mock:EisPrivateKey is not configured");

        AuthCredentials authCredentials = RSAHelper.DecryptBase64Rsa<AuthCredentials>(authBody.data, privateKey);

        // Check the credentials before a session is created for the tokens
        var credentialsError = AuthValidator.ValidateCredentials(authCredentials.userId, authCredentials.password, applicationId);
        if (credentialsError is not null)
            return new AuthResult(null, credentialsError);

        var tokens = tokenIssuer.IssueTokens();

        var EisReply = new EisReply
        {
          accreditationId = accreditationId,
          userId = authCredentials.userId,
          authToken = tokens.AuthToken,
          sessionKey = tokens.SessionKey,
          tokenExpiry = tokens.TokenExpirty  
        };

        var EisReplyJson = JsonSerializer.Serialize(EisReply);

        string data = Aes256CbcHelper.Encrypt(EisReplyJson, authCredentials.authKey);

        return new AuthResult(data, null);
    }
}
