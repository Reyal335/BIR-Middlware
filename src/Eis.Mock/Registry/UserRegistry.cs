namespace Eis.Mock.Registry;

public record TestUser(string password, HashSet<string> ApplicationIds);

public class UserRegistry
{
    public static readonly IReadOnlyDictionary<string, TestUser> Users =
        new Dictionary<string, TestUser>
        {
            ["mockuser"] = new ("Test123", new() { "APP-GAP", "APP-TEST-002" })
        };
}