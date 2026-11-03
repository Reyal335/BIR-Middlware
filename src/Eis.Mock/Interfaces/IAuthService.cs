using Eis.Mock.Auth;
using Eis.Mock.DTO;

namespace Eis.Mock.Interfaces;

public sealed record AuthResult(TokenBody? Tokens, ErrorResponse? Error);

public interface IAuthService
{
    AuthResult Authenticate(string applicationId, string accreditationId, string datetime, AuthBody authBody);
}
