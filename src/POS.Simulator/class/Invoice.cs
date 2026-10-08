using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Bogus.DataSets;

namespace POS.Simulator.@class;

public class POSInvoice
{
    [JsonPropertyName("posTerminalId")]
    public string PosTerminalId { get; set; } = string.Empty;

    [JsonPropertyName("storeCode")]
    public string StoreCode { get; set; } = string.Empty;
    
    [JsonPropertyName("transactionId")]
    public string TransactionId { get; set; } = string.Empty;

    [JsonPropertyName("invoiceType")]
    public string InvoiceType { get; set; } = "SALES_INVOICE";

    [JsonPropertyName("transactionDateTime")]
    public DateTimeOffset TransactionDateTime { get; set; } = new DateTimeOffset();

    [JsonPropertyName("lineItems")]
    public List<PosLineItems> LineItems { get; set; } = [];

}

public class Customer
{
    
}

public class PosLineItems
{
    
}


