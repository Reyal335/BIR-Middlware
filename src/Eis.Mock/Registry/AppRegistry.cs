using Microsoft.AspNetCore.StaticFiles;

namespace Eis.Mock.Registry;

public record RegisteredApp(string AccreditationId, string ApplicationId, bool IsActive);

public class AppRegistry
{
    public static readonly IReadOnlyDictionary<string, RegisteredApp> Apps =
    new Dictionary<string, RegisteredApp>
    {
            ["APP-TEST-001"] = new("ACC-TEST-001", "APP-GAP", true),
            ["APP-TEST-002"] = new("ACC-TEST-001", "test-secret-2", false),
    };
}