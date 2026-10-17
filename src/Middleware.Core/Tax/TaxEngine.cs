using Microsoft.VisualBasic;
using Middleware.Core.DTO;
using Middleware.Core.Models;

namespace Middleware.Core.Tax;


public static class TaxEngine
{
    const decimal VatDivisor = 1.12m;
    const decimal ScPwdRate = 0.20m;

    public static TaxComputationResult Compute(POSInvoice Invoice)
    {
        var errors = new List<string>();
        var lines = new List<LineAmounts>();
        var sellerIsVat = Invoice.Seller.VatRegistration == "VAT";
        
        foreach (var line in Invoice.LineItems)
        {
            if(!validateInvoice(line, Invoice.Customer, sellerIsVat, out var lineError))
            {
                errors.Add(lineError);
                continue;
            }
            lines.Add(ComputeLine(Invoice.Seller, line));

        }

        return new TaxComputationResult();
    }

    private static bool validateInvoice(
        PosLineItems line, Customer customer, bool sellerIsVat, out string Error)
    {
        decimal gross = (decimal)Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
        decimal lineAmount = (decimal)Math.Round(line.Quantity * line.UnitPrice, 2, MidpointRounding.AwayFromZero);
        if (gross != lineAmount)
        {
            Error = "Gross Calculation did not match";
            return false;
        }
        if (line.Vatable && line.TaxType is not null)
        {
            Error = $"Line no {line.LineNo} cannot be vatable and having a tax type {line.TaxType}";
            return false;
        }
        Error = "Fuck you";
        return true;
    }

    private static LineAmounts ComputeLine(Seller seller, PosLineItems lineItem)
    {
        if (seller.VatRegistration == "NON_VAT")
        {
            return  LineAmounts.NonVat(lineItem.LineNo, (decimal)lineItem.LineAmount);
        }

        return lineItem.TaxType switch
        {
            taxType.TAX_TYPE => LineAmounts.Vatable(lineItem.LineNo, (decimal)lineItem.LineAmount, VatDivisor),
            taxType.ZERO_RATED => LineAmounts.ZeroRated(lineItem.LineNo, (decimal)lineItem.LineAmount, VatDivisor),
            taxType.VAT_EXEMPT => LineAmounts.VatExempt(lineItem.LineNo, (decimal)lineItem.LineAmount, VatDivisor),
            taxType.VAT_EXEMPT_SCPWD => LineAmounts.VatExemptScPwd(lineItem.LineNo, (decimal)lineItem.LineAmount, VatDivisor, ScPwdRate),
            _ => LineAmounts.Vatable(lineItem.LineNo, (decimal)lineItem.LineAmount, VatDivisor)
        };
    }
}