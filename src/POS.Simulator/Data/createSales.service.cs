namespace POS.Simulator.Data;
using POS.Simulator.@class;
using POS.Simulator.Data.Factories;

public static class CreateSalesService
{
    public static POSInvoice CreateOrders ()
    {
        var invoice = new FakeInvoice();
        
        POSInvoice fakeInvoice = invoice.Generate();
        return fakeInvoice;
    }
}