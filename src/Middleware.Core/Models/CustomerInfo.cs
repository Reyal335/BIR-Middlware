namespace Middleware.Core.Models;

public class CustomerInfo
{
    public string Type { get; init; } = "";
    public string? RegisteredName { get; init; }
    public string? Tin { get; init; }
    public string? Address { get; init; }
}