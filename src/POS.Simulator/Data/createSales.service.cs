namespace POS.Simulator.Data;
using POS.Simulator.@class;
using POS.Simulator.Data.Factories;

public static class CreateSalesService
{
    public static IReadOnlyCollection<POSInvoice> CreateOrders ()
    {
        var invoices = new FakeInvoice();
        return invoices.Generate(1);
    }
}