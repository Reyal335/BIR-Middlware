using Middleware.Core.Models;

namespace Middleware.Core.Eis;

public class EisMapper
{
    public BirEisInvoicePayload Map(Invoice invoice)
    {
        
        List<EisLineItem> items = [];

        foreach (var item in invoice.Lines)
        {
            items.Add(new EisLineItem
            {
                LineNo = item.LineNo,
                Name = item.Sku ?? "Unnamed item",
                Qty = item.Quantity,
                UnitCost = item.UnitPrice,
                Discount = 0,
                SalesAmt = item.LineAmount,
                NetSales = item.NetSales,
                TaxType = item.TaxType.ToString()
            });
        }

        return new BirEisInvoicePayload
        {
            EisUniqueId = invoice.EisUniqueId,
            DocumentType = invoice.InvoiceType,
            Header = new EisHeader
            {
                InvoiceNumber = invoice.InvoiceNumber,
                InvoiceDate = TimeZoneInfo.ConvertTime(invoice.TransactionDateTime, TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")).Date.ToString("yyyy-MM-dd"),
                InvoiceTime = TimeZoneInfo.ConvertTime(invoice.TransactionDateTime, TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila")).ToString("HH:mm:sszzz"),
                Currency = "PHP"
            },
            Seller = new EisParty
            {
                Tin = invoice.Seller.Tin,
                BranchCode = invoice.Seller.BranchCode,
                RegisteredName = invoice.Seller.RegisteredName,
                TradeName = invoice.Seller.TradeName,
                Address = invoice.Seller.Address,
                VatRegistration = invoice.Seller.VatRegistration,
                AccreditationNo = invoice.Seller.PtuOrAccreditationNo
            },
            Buyer = new EisParty
            {
                Tin = invoice.Customer.Tin,
                RegisteredName = invoice.Customer.RegisteredName ?? $"{invoice.Customer.Type} CUSTOMER",
                Address = invoice.Customer.Address,
                BuyerType = invoice.Customer.Type
            },
            ItemList = items
        };
    }
}