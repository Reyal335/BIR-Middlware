using Bogus;
using POS.Simulator.@class;

namespace POS.Simulator.Data.Factories;

public enum InvoiceType
{
    SALES_INVOICE,
    SERVICE_INVOICE,
    CREDIT_NOTE,
    DEBIT_NOTE
}

public sealed class FakeInvoice : Faker<POSInvoice>
{
    private static readonly FakeFactorySeller _factorySeller = new ();
    private static readonly FakeFactoryCustomer _factoryCustomer = new ();
    private static readonly FakeFactoryPosLineItem _factoryPosLineItem = new ();
    public FakeInvoice()
    {
        RuleFor(i => i.PosTerminalId, faker => faker.Random.Replace("POS-###"));
        RuleFor(i => i.StoreCode, faker => faker.Random.Replace("BR-###"));
        RuleFor(i => i.TransactionId, faker => faker.Random.Replace("TXN-########-######"));
        RuleFor(i => i.InvoiceType, faker => faker.PickRandom<InvoiceType>().ToString());
        RuleFor(i => i.InvoiceNumber, faker => faker.Random.Replace("SI-####-######"));
        RuleFor(i => i.TransactionDateTime, _ => DateTimeOffset.Now);
        RuleFor(i => i.Cashier, faker => faker.Name.FullName());

        // Generate Seller
        RuleFor(i => i.Seller, _ => _factorySeller.Generate());

        // Generate Customer
        RuleFor(i => i.Customer, _ => _factoryCustomer.Generate());

        // Generate LineItems
        RuleFor(i => i.LineItems, _ => _factoryPosLineItem.Generate(Random.Shared.Next(1, 100)));
    }
}