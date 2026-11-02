namespace Eis.Mock.Registry;

public record TestUser(string password, HashSet<string> ApplicationIds);

public class UserRegistry
{
    public static readonly IReadOnlyDictionary<string, TestUser> Users =
        new Dictionary<string, TestUser>
        {
            ["testUser1"] = new ("Test123", new() { "APP-TEST-001", "APP-TEST-002" })
        };
}