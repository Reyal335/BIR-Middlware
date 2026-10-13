using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Middleware.Core.DTO;

public class POSInvoice
{
    [JsonPropertyName("PosTerminalId")]
    public string PosTerminalId { get; set; } = string.Empty;

    [JsonPropertyName("StoreCode")]
    public string StoreCode { get; set; } = string.Empty;
    
    [JsonPropertyName("TransactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("InvoiceType")]
    public string InvoiceType { get; set; } = "SALES_INVOICE";

    [JsonPropertyName("InvoiceNumber")]
    public string InvoiceNumber { get; set; } = string.Empty;

    [JsonPropertyName("TransactionDateTime")]
    public DateTimeOffset TransactionDateTime { get; set; } = new DateTimeOffset();

    [JsonPropertyName("Cashier")]
    public string Cashier { get; set; } = string.Empty;

    [JsonPropertyName("Seller")]
    public Seller Seller { get; set; } = new Seller();

    [JsonPropertyName("Customer")]
    public Customer Customer { get; set; } = new Customer();

    [JsonPropertyName("LineItems")]
    public List<PosLineItems> LineItems { get; set; } = [];

    [JsonPropertyName("Totals")]
    public Totals Totals { get; set; } = new Totals();

    [JsonPropertyName("Payment")]
    public Payment Payment { get; set; } = new Payment();
}

public class Seller
{
    [JsonPropertyName("RegisteredName")]
    public string RegisteredName { get; set; } = string.Empty;

    [JsonPropertyName("TradeName")]
    public string TradeName { get; set; } = string.Empty;

    [JsonPropertyName("Tin")]
    public string Tin { get; set; } = string.Empty;

    [JsonPropertyName("BranchCode")]
    public string BranchCode { get; set; } = string.Empty;

    [JsonPropertyName("Address")]
    public string Address { get; set; } = string.Empty;

    [JsonPropertyName("VatRegistration")]
    public string VatRegistration { get; set; } = string.Empty;

    [JsonPropertyName("PtuOrAccerditationNo")]
    public string PtuOrAccerditationNo { get; set; } = string.Empty;

    [JsonPropertyName("MachineAccreditationNo")]
    public string MachineAccreditationNo { get; set; } = string.Empty;
}

public class Customer
{
    [JsonPropertyName("Type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("RegisteredName")]
    public string? RegisteredName { get; set; } = null;

    [JsonPropertyName("Tin")]
    public string? Tin { get; set; } = null; 

    [JsonPropertyName("Address")]
    public string? Address { get; set; } = null;
}

public class PosLineItems
{
    [JsonPropertyName("LineNo")]
    public int LineNo { get; set; } = 0; 

    [JsonPropertyName("Sku")]
    public string? SKU { get; set; } = string.Empty; 

    [JsonPropertyName("Description")]
    public string? Description { get; set; } = string.Empty; 

    [JsonPropertyName("Quantity")]
    public int? Quantity { get; set; } = 0; 

    [JsonPropertyName("UnitPrice")]
    public double UnitPrice { get; set; } = 0; 

    [JsonPropertyName("LineAmount")]
    public double LineAmount { get; set; } = 0;

    [JsonPropertyName("Vatable")]
    public Boolean Vatable { get; set; } = true;  

}

public class Totals 
{
    [JsonPropertyName("GrossAmount")]
    public double GrossAmount { get; set; } = 0;

    [JsonPropertyName("VatableSales")]
    public double VatableSales { get; set; } = 0;

    [JsonPropertyName("VatExemptSales")]
    public double VatExemptSales { get; set; } = 0;
    
    [JsonPropertyName("ZeroRelatedSales")]
    public double ZeroRelatedSales { get; set; } = 0;
    
    [JsonPropertyName("VatAmount")]
    public double VatAmount { get; set; } = 0;
    
    [JsonPropertyName("ScPwdDiscount")]
    public double ScPwdDiscount { get; set; } = 0;
    
    [JsonPropertyName("OtherDiscount")]
    public double OtherDiscount { get; set; } = 0;
    
    [JsonPropertyName("TotalDue")]
    public double TotalDue { get; set; } = 0;
}

public class Payment {

    [JsonPropertyName("Method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("AmountTendered")]
    public double AmountTendered { get; set; } = 0;
    
    [JsonPropertyName("Change")]
    public double Change { get; set; } = 0;
}

