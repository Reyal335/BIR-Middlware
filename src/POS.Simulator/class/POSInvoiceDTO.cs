using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using Bogus.DataSets;
using System.Text.Json;

namespace POS.Simulator.@class;

public class POSInvoice
{
    public string PosTerminalId { get; set; } = string.Empty;

    public string StoreCode { get; set; } = string.Empty;

    public string TransactionId { get; set; } = string.Empty;

    public string InvoiceType { get; set; } = "SALES_INVOICE";

    public string InvoiceNumber { get; set; } = string.Empty;

    public DateTimeOffset TransactionDateTime { get; set; } = new DateTimeOffset();

    public string Cashier { get; set; } = string.Empty;

    public Seller Seller { get; set; } = new Seller();

    public Customer Customer { get; set; } = new Customer();

    public List<PosLineItems> LineItems { get; set; } = [];

    public Totals Totals { get; set; } = new Totals();

    public Payment Payment { get; set; } = new Payment();

    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class Seller
{
    public string RegisteredName { get; set; } = string.Empty;

    public string TradeName { get; set; } = string.Empty;

    public string Tin { get; set; } = string.Empty;

    public string BranchCode { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public string VatRegistration { get; set; } = string.Empty;

    public string PtuOrAccerditationNo { get; set; } = string.Empty;

    public string MachineAccreditationNo { get; set; } = string.Empty;
}

public class Customer
{
    public string Type { get; set; } = string.Empty;

    public string? RegisteredName { get; set; } = null;

    public string? Tin { get; set; } = null; 

    public string? Address { get; set; } = null;
}

public class PosLineItems
{
    public int LineNo { get; set; } = 0; 

    public string? Sku { get; set; } = string.Empty; 

    public string? Description { get; set; } = string.Empty; 

    public int? Quantity { get; set; } = 0; 

    public double UnitPrice { get; set; } = 0; 

    public double LineAmount { get; set; } = 0;

    public bool Vatable { get; set; } = true;  

}

public class Totals 
{
    public double GrossAmount { get; set; } = 0;

    public double VatableSales { get; set; } = 0;

    public double VatExemptSales { get; set; } = 0;

    public double ZeroRelatedSales { get; set; } = 0;

    public double VatAmount { get; set; } = 0;

    public double ScPwdDiscount { get; set; } = 0;

    public double OtherDiscount { get; set; } = 0;

    public double TotalDue { get; set; } = 0;
}

public class Payment {
    public string Method { get; set; } = string.Empty;

    public double AmountTendered { get; set; } = 0;

    public double Change { get; set; } = 0;
}

