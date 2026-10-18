using Microsoft.VisualBasic;
using Middleware.Core.DTO;
using Middleware.Core.Models;
using System.Linq;

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

        // Tax Computation fails if there is at least one error
        if (errors.Count != 0) return TaxComputationResult.Failed(errors);


        decimal GrossAmount = lines.Sum(l => l.Gross);
        decimal VatableSales = lines.Sum(l => l.VatableSales);
        decimal VatExemptSales = lines.Sum(l => l.VatExemptSales);
        decimal ZeroRatedSales = lines.Sum(l => l.ZeroRatedSales);
        decimal VatAmount = lines.Sum(l => l.VatAmount);
        decimal ScPwdDiscount = lines.Sum(l => l.ScPwdDiscount);
        decimal TotalDue = VatableSales + VatAmount + VatExemptSales + ZeroRatedSales - ScPwdDiscount;

        if(TotalDue != (decimal)Invoice.Totals.TotalDue)
        {
            errors.Add("POS sent total due is not equal to the calculated total due");
            return TaxComputationResult.Failed(errors);
        }

        var totals = new InvoiceTotals
        {
            GrossAmount = GrossAmount,
            VatableSales = VatableSales,
            VatExemptSales = VatExemptSales,
            ZeroRatedSales = ZeroRatedSales,
            VatAmount = VatAmount,
            ScPwdDiscount = ScPwdDiscount,
            TotalDue = TotalDue
            
        };
        return TaxComputationResult.Succeeded(lines, totals);
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

        if ((customer.Type != "SENIOR_CITIZEN" || customer.Type != "PWD")
            && line.TaxType == taxType.VAT_EXEMPT_SCPWD)
        {
            Error = $"Customer must be a senior citizen or pwd to have a tax type of vat exempted for SCPWD";
            return false;
        }

        if (customer.Type != "REGISTERED_BUSINESSS" && line.TaxType == taxType.VAT_EXEMPT)
        {
            Error = $"Customer must be a registered business to have a tax type of VAT_EXEMPT";
            return false;
        }

        if (sellerIsVat && line.TaxType == taxType.ZERO_RATED)
        {
            Error = $"Seller must be vat registered for Line No: {line.LineNo} to be zero rated";
        }
        Error = "No error";
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