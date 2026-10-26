// Middleware.Core/Models/Invoice.cs
using Middleware.Core.DTO;
using Middleware.Core.Models;
using Middleware.Core.Tax;

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
    public required string EisUniqueId { get; init; }
    public string? SignedJws { get; private set; }
    public DateTimeOffset? SignedAt { get; private set; }
    private Invoice() { } // EF only

    // ── Creation: DTO -> domain ───────────────────────────
    public static Invoice FromPos(POSInvoice dto, TaxComputationResult tax, string eisId)
    {
        var computed = tax.Lines.ToDictionary(l => l.LineNo);

        var invoice = new Invoice
        {
            StoreCode = dto.StoreCode,
            PosTerminalId = dto.PosTerminalId,
            TransactionId = dto.TransactionId,
            InvoiceType = dto.InvoiceType,
            InvoiceNumber = dto.InvoiceNumber,
            TransactionDateTime = dto.TransactionDateTime,
            Cashier = dto.Cashier,
            EisUniqueId = eisId,
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
            Totals = tax.Totals!,
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
            var c = computed[l.LineNo];
            invoice._lines.Add(new InvoiceLine
            {
                LineNo = l.LineNo,
                Sku = l.SKU,
                Description = l.Description,
                Quantity = l.Quantity,
                UnitPrice = (decimal)l.UnitPrice,
                LineAmount = (decimal)l.LineAmount,
                TaxType = ToTaxClass(l.Vatable, l.TaxType),
                VatableSales = c.VatableSales,
                VatExemptSales = c.VatExemptSales,
                ZeroRatedSales = c.ZeroRatedSales,
                VatAmount = c.VatAmount,
                ScPwdDiscount = c.ScPwdDiscount,
                NonVatSales = c.NonVatSales,
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

    public void MarkSubmitted()
    {
        if (Status != InvoiceStatus.Queued)
            throw new InvalidOperationException($"Cannot mark submitted from {Status}");
        Status = InvoiceStatus.Submitting;
    }

    // Save JWS and time signed at
    public void AttachSignature(string jws)
    {
        if (Status != InvoiceStatus.Submitting)
            throw new InvalidOperationException($"Cannot mark save jws data from {Status}");
        SignedJws = jws;
        SignedAt = DateTimeOffset.Now;
    }

    //
    public void ApplyEisResult(bool accepted, string? eisUniqueId)
    {
        if (Status != InvoiceStatus.Queued)
            throw new InvalidOperationException($"Cannot apply EIS result from {Status}");
        AttemptCount++;
        Status = accepted ? InvoiceStatus.Accepted : InvoiceStatus.Rejected;
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
