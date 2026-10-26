

namespace Middleware.Core.Models;
public class InvoiceLine
{
    public int LineNo { get; init; }
    public string? Sku { get; init; }
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
    public TaxType TaxType { get; init; }

    // Computed by TaxEngine
    public decimal VatableSales { get; init; }
    public decimal VatExemptSales { get; init; }
    public decimal ZeroRatedSales { get; init; }
    public decimal VatAmount { get; init; }
    public decimal ScPwdDiscount { get; init; }
    public decimal NonVatSales { get; init; }

    // Derived, not stored (getter-only, so EF does not map it)
    public decimal NetSales =>
        VatableSales + VatAmount + VatExemptSales + ZeroRatedSales + NonVatSales - ScPwdDiscount;
}
