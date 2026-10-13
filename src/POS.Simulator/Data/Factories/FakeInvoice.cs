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

    // Properties that dictate constraints of other properties in other classes
    private CustomerType customerType;

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
        RuleFor(i => i.Customer, _ => {
            Customer customer = _factoryCustomer.Generate();
            customerType = customer.Type;
            return customer;
        });
        // Generate LineItems — the factory must be built here, inside the rule, so it sees
        // the customerType set by the Customer rule above (rules only run on Generate()).
        RuleFor(i => i.LineItems, _ => new FakeFactoryPosLineItem(customerType).Generate(Random.Shared.Next(1, 10)));
    }
}