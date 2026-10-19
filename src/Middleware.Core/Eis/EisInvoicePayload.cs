using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BirMiddleware.Models.Eis
{
    /// <summary>
    /// Shape of the payload your middleware SENDS to the BIR EIS, after transforming
    /// a PosInvoice. Field names/casing here follow the publicly documented EIS shape
    /// (Header / Seller / Buyer / ItemList / TaxSummary, JWS-signed). BIR does not publish
    /// this as an open spec — once you enroll in EIS Certification you'll get the real
    /// sandbox contract, which may differ in naming/casing. Keep this behind an interface
    /// (IEisInvoiceMapper) so swapping the exact field names later is a one-file change.
    /// </summary>
    public class BirEisInvoicePayload
    {
        /// <summary>24-digit unique ID, typically Seller TIN + Branch + sequence, scheme TBD by BIR.</summary>
        [JsonPropertyName("EISUniqueID")]
        public string EisUniqueId { get; set; } = string.Empty;

        [JsonPropertyName("DocumentType")]
        public string DocumentType { get; set; } = "SALES_INVOICE"; // SALES_INVOICE | OFFICIAL_RECEIPT | DEBIT_NOTE | CREDIT_NOTE

        [JsonPropertyName("TransactionType")]
        public string TransactionType { get; set; } = "SALE";

        [JsonPropertyName("Header")]
        public EisHeader Header { get; set; } = new();

        [JsonPropertyName("Seller")]
        public EisParty Seller { get; set; } = new();

        [JsonPropertyName("Buyer")]
        public EisParty Buyer { get; set; } = new();

        [JsonPropertyName("ItemList")]
        public List<EisLineItem> ItemList { get; set; } = new();

        [JsonPropertyName("TaxSummary")]
        public EisTaxSummary TaxSummary { get; set; } = new();

        [JsonPropertyName("Signature")]
        public EisSignature Signature { get; set; } = new();
    }

    public class EisHeader
    {
        [JsonPropertyName("InvoiceNumber")]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>yyyy-MM-dd</summary>
        [JsonPropertyName("InvoiceDate")]
        public string InvoiceDate { get; set; } = string.Empty;

        /// <summary>HH:mm:sszzz</summary>
        [JsonPropertyName("InvoiceTime")]
        public string InvoiceTime { get; set; } = string.Empty;

        [JsonPropertyName("Currency")]
        public string Currency { get; set; } = "PHP";
    }

    public class EisParty
    {
        /// <summary>9 numeric digits, no dashes. Null for a walk-in buyer.</summary>
        [JsonPropertyName("Tin")]
        public string? Tin { get; set; }

        [JsonPropertyName("BranchCode")]
        public string? BranchCode { get; set; }

        [JsonPropertyName("RegisteredName")]
        public string RegisteredName { get; set; } = string.Empty;

        [JsonPropertyName("TradeName")]
        public string? TradeName { get; set; }

        [JsonPropertyName("Address")]
        public string? Address { get; set; }

        [JsonPropertyName("VatRegistration")]
        public string? VatRegistration { get; set; }

        [JsonPropertyName("AccreditationNo")]
        public string? AccreditationNo { get; set; }

        /// <summary>Buyer-only: SENIOR_CITIZEN | PWD | REGISTERED_BUSINESS | WALK_IN</summary>
        [JsonPropertyName("BuyerType")]
        public string? BuyerType { get; set; }

        /// <summary>Buyer-only: SC/PWD ID number backing a discount line.</summary>
        [JsonPropertyName("DiscountIdNo")]
        public string? DiscountIdNo { get; set; }
    }

    public class EisLineItem
    {
        [JsonPropertyName("LineNo")]
        public int LineNo { get; set; }

        [JsonPropertyName("Nm")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("Qty")]
        public decimal Qty { get; set; }

        [JsonPropertyName("UnitCost")]
        public decimal UnitCost { get; set; }

        [JsonPropertyName("Discount")]
        public decimal Discount { get; set; }

        [JsonPropertyName("SalesAmt")]
        public decimal SalesAmt { get; set; }

        [JsonPropertyName("NetSales")]
        public decimal NetSales { get; set; }

        /// <summary>VATABLE | ZERO_RATED | VAT_EXEMPT | VAT_EXEMPT_SCPWD</summary>
        [JsonPropertyName("TaxType")]
        public string TaxType { get; set; } = "VATABLE";
    }

    public class EisTaxSummary
    {
        [JsonPropertyName("VATableSales")]
        public decimal VatableSales { get; set; }

        [JsonPropertyName("VATExemptSales")]
        public decimal VatExemptSales { get; set; }

        [JsonPropertyName("ZeroRatedSales")]
        public decimal ZeroRatedSales { get; set; }

        [JsonPropertyName("VATAmount")]
        public decimal VatAmount { get; set; }

        [JsonPropertyName("SCPWDDiscount")]
        public decimal ScPwdDiscount { get; set; }

        [JsonPropertyName("OtherDiscount")]
        public decimal OtherDiscount { get; set; }

        [JsonPropertyName("TotalAmountDue")]
        public decimal TotalAmountDue { get; set; }
    }

    public class EisSignature
    {
        [JsonPropertyName("Type")]
        public string Type { get; set; } = "JWS";

        /// <summary>Compact JWS: header.payload.signature, base64url-encoded.</summary>
        [JsonPropertyName("Value")]
        public string Value { get; set; } = string.Empty;
    }
}