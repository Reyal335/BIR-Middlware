

namespace Middleware.Core.Models;
public class InvoiceLine
{
    public int LineNo { get; init; }
    public string? Sku { get; init; }
    public string? Description { get; init; }
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineAmount { get; init; }
    public TaxClass TaxClass { get; init; }
}
