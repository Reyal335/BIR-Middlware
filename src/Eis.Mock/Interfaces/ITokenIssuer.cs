namespace Eis.Mock.Interfaces;

public sealed record TokenBody(
    string AuthToken, string SessionKey, string TokenExpirty);

public interface ITokenIssuer
{
    TokenBody IssueTokens();
}