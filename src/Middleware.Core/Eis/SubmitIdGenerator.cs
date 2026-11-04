using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace Middleware.Infra.Eis;

public static class SubmitIdGenerator
{
    public static string Create(string certId, DateTimeOffset now)
    {
        string suffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(2)); // e.g. "7F3A"
        return $"{certId}-{now:yyyyMMddHHmmss}-{suffix}";
    }
}