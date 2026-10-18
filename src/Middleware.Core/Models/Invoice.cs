// Middleware.Core/Models/Invoice.cs
using Middleware.Core.DTO;
using Middleware.Core.Models;

namespace Middleware.Core.Models;

public class Invoice
{
    // ── Identity ─────────────────────────────────────────
    public int Id { get; private set; }

    // ── Header (from the DTO) ────────────────────────────
    public string StoreCode { get; private set; } = "";
    public string PosTerminalId { get; private set; } = "";
    public string TransactionId { get; private set; } = "";
    public string InvoiceType { get; private set; } = "";
    public string InvoiceNumber { get; private set; } = "";
    public DateTimeOffset TransactionDateTime { get; private set; }
    public string Cashier { get; private set; } = "";

    // ── Grouped values (from the DTO) → owned types ──────
    public SellerInfo Seller { get; private set; } = null!;
    public CustomerInfo Customer { get; private set; } = null!;
    public InvoiceTotals Totals { get; private set; } = null!;
    public PaymentInfo Payment { get; private set; } = null!;

    private readonly List<InvoiceLine> _lines = [];
    public IReadOnlyList<InvoiceLine> Lines => _lines;

    // ── Workflow (NOT in the DTO — middleware owns these) ─
    public InvoiceStatus Status { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTimeOffset? NextRetryAt { get; private set; }
    public string? EisUniqueId { get; private set; }

    private Invoice() { } // EF only

    // ── Creation: DTO -> domain ───────────────────────────
    public static Invoice FromPos(POSInvoice dto, InvoiceTotals totals)
    {
        var invoice = new Invoice
        {
            StoreCode = dto.StoreCode,
            PosTerminalId = dto.PosTerminalId,
            TransactionId = dto.TransactionId,
            InvoiceType = dto.InvoiceType,
            InvoiceNumber = dto.InvoiceNumber,
            TransactionDateTime = dto.TransactionDateTime,
            Cashier = dto.Cashier,
            Seller = new SellerInfo
            {
                RegisteredName = dto.Seller.RegisteredName,
                TradeName = dto.Seller.TradeName,
                Tin = dto.Seller.Tin,
                BranchCode = dto.Seller.BranchCode,
                Address = dto.Seller.Address,
                VatRegistration = dto.Seller.VatRegistration,
                PtuOrAccreditationNo = dto.Seller.PtuOrAccerditationNo,
                MachineAccreditationNo = dto.Seller.MachineAccreditationNo,
            },
            Customer = new CustomerInfo
            {
                Type = dto.Customer.Type,
                RegisteredName = dto.Customer.RegisteredName,
                Tin = dto.Customer.Tin,
                Address = dto.Customer.Address,
            },
            Totals = totals,
            Payment = new PaymentInfo
            {
                Method = dto.Payment.Method,
                AmountTendered = (decimal)dto.Payment.AmountTendered,
                Change = (decimal)dto.Payment.Change,
            },
            Status = InvoiceStatus.Received,
            ReceivedAt = DateTimeOffset.UtcNow,
        };

        foreach (var l in dto.LineItems)
        {
            invoice._lines.Add(new InvoiceLine
            {
                LineNo = l.LineNo,
                Sku = l.SKU,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = (decimal)l.UnitPrice,
                LineAmount = (decimal)l.LineAmount,
                TaxType = ToTaxClass(l.Vatable, l.TaxType),
            });
        }

        return invoice;
    }

    // ── Behavior ─────────────────────────────────────────
    public void MarkQueued()
    {
        if (Status != InvoiceStatus.Received)
            throw new InvalidOperationException($"Cannot queue from {Status}");
        Status = InvoiceStatus.Queued;
    }

    public void ApplyEisResult(bool accepted, string? eisUniqueId)
    {
        if (Status != InvoiceStatus.Queued)
            throw new InvalidOperationException($"Cannot apply EIS result from {Status}");
        AttemptCount++;
        Status = accepted ? InvoiceStatus.Accepted : InvoiceStatus.Rejected;
        EisUniqueId = eisUniqueId;
    }

    private static TaxType ToTaxClass(bool vatable, taxType? t) => vatable
        ? TaxType.VATABLE
        : t switch
        {
            taxType.ZERO_RATED       => TaxType.ZERO_RATED,
            taxType.VAT_EXEMPT_SCPWD => TaxType.VAT_EXEMPT_SCPWD,
            _                        => TaxType.VAT_EXEMPT,
        };
}
