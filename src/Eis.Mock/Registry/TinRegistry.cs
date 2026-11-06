namespace Eis.Mock.Registry;

public class TinRegistry
{
    /// <summary>Seller TINs (9 digits, no dashes) registered with the mock EIS.</summary>
    public static readonly IReadOnlySet<string> Tins = new HashSet<string>
    {
        "123456789",
        "987654321",
        "111222333",
    };
}
