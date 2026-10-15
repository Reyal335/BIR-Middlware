namespace Middleware.Core.Models;

public class PaymentInfo
{
    public string Method { get; init; } = "";
    public decimal AmountTendered { get; init; }
    public decimal Change { get; init; }
}