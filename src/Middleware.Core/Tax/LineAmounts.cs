using Middleware.Core.Models;

namespace Middleware.Core.Tax;


public sealed class LineAmounts
{
    public int LineNo { get; private init; }
    public decimal Gross { get; private init; }
    public decimal VatableSales { get; private init; }
    public decimal VatExemptSales { get; private init; }
    public decimal ZeroRatedSales { get; private init; }
    public decimal VatAmount { get; private init; }
    public decimal ScPwdDiscount { get; private init; }

    private LineAmounts() {}

    public static LineAmounts Vatable(int lineNo, decimal gross, decimal vatRate)
    {
        var net = Math.Round(gross / 1 + vatRate, 2, MidpointRounding.AwayFromZero);

        return new LineAmounts
        {
            LineNo = lineNo,
            VatableSales = net,
            Gross = gross,
            VatAmount = Math.Round(gross - net, 2, MidpointRounding.AwayFromZero)
        };
    }

    public static LineAmounts ZeroRated(int lineNo, decimal gross, decimal vatRate)
    {

        return new LineAmounts
        {
            LineNo = lineNo,
            ZeroRatedSales = gross,
            Gross = gross
        };  
    }

    public static LineAmounts VatExempt(int lineNo, decimal gross, decimal vatRate)
    {
        return new LineAmounts
        {
            LineNo = lineNo,
            ZeroRatedSales = gross,
            Gross = gross
        };  
    }

    public static LineAmounts VatExemptScPwd(int LineNo, decimal gross, decimal vatRate, decimal scDiscount)
    {
        var net = Math.Round(gross / 1 + vatRate, 2, MidpointRounding.AwayFromZero);
        var scPwdDiscount = Math.Round(net * scDiscount, 2, MidpointRounding.AwayFromZero);
        return new LineAmounts
        {
            LineNo = LineNo,
            Gross = gross,
            VatExemptSales = net,
            ScPwdDiscount = scPwdDiscount,
        };
    }

    public static LineAmounts NonVat(int lineNo, decimal gross) => new()
    {
        LineNo = lineNo,
        Gross = gross
    };


}

public sealed class InvoiceTotals
{
    public decimal GrossAmount { get; init; }
    public decimal VatableSales { get; init; }
    public decimal VatExemptSales { get; init; }
    public decimal ZeroRatedSales { get; init; }
    public decimal VatAmount { get; init; }
    public decimal ScPwdDiscount { get; init; }
    public decimal TotalDue { get; init; }
}

public sealed class TaxComputationResult
{
    public bool IsSuccess { get; private init; }
    public IReadOnlyList<string> Errors { get; private init; } = Array.Empty<string>();
    public IReadOnlyList<LineAmounts> Lines { get; private init; } = Array.Empty<LineAmounts>();
    public InvoiceTotals? Totals { get; private init; }
 
    public static TaxComputationResult Succeeded(IReadOnlyList<LineAmounts> lines, InvoiceTotals totals) =>
        new() { IsSuccess = true, Lines = lines, Totals = totals };
 
    public static TaxComputationResult Failed(IReadOnlyList<string> errors) =>
        new() { IsSuccess = false, Errors = errors };
}