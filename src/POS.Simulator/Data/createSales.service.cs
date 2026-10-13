namespace POS.Simulator.Data;
using POS.Simulator.@class;
using POS.Simulator.Data.Factories;

public static class CreateSalesService
{
    public static POSInvoice CreateOrders ()
    {
        var invoice = new FakeInvoice();
        
        POSInvoice fakeInvoice = invoice.Generate();

        Totals totals = calculateTotalVat(fakeInvoice);
        fakeInvoice.Totals = totals;

        return fakeInvoice;
    }

    public static Totals calculateTotalVat (POSInvoice posInvoice)
    {
        Totals totals = new();

        /** If seller is a non vat seller, short circuit everything and vatable and taxtype
            flags are irrelevant and ignored
        **/
        if (posInvoice.Seller.VatRegistration == "NON_VAT")
        {
            foreach (PosLineItems item in posInvoice.LineItems) {
                totals.GrossAmount += item.LineAmount;
                totals.TotalDue += item.LineAmount;
            }   
            return totals;
        }

        // Go through the calculation for every item 
        foreach (PosLineItems item in posInvoice.LineItems)
        {
            totals.GrossAmount += item.LineAmount;
            if (item.TaxType is null)
            {
                // Item is not vatable and no tax type, no bucket it belongs to
                if (!item.Vatable) continue;

                totals.VatableSales += item.LineAmount/1.12;
                totals.VatAmount += item.LineAmount - (item.LineAmount/1.12);
                continue;
            }

            // Tax type will be one of three types
            if (item.TaxType == taxType.ZERO_RATED)
            {
                totals.ZeroRelatedSales += item.LineAmount;
            } else if (item.TaxType == taxType.VAT_EXEMPT)
            {
                totals.VatExemptSales += item.LineAmount;
            } else if (item.TaxType == taxType.VAT_EXEMPT_SCPWD)
            {
                totals.VatExemptSales += item.LineAmount/1.12;
                totals.ScPwdDiscount += item.LineAmount/1.12 * .20;
            }
        }

        totals.TotalDue = totals.VatableSales + totals.VatAmount + totals.VatExemptSales
            + totals.ZeroRelatedSales - totals.ScPwdDiscount - totals.OtherDiscount;

        return totals;

    }
}