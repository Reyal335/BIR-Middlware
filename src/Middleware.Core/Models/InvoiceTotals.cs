namespace Middleware.Core.Models;

public class InvoiceTotals
{
    public decimal GrossAmount { get; init; }
    public decimal VatableSales { get; init; }
    public decimal VatExemptSales { get; init; }
    public decimal ZeroRatedSales { get; init; }
    public decimal VatAmount { get; init; }
    public decimal ScPwdDiscount { get; init; }
    public decimal NonVatSales { get; init; }
    public decimal OtherDiscount { get; init; }
    public decimal TotalDue { get; init; }
}