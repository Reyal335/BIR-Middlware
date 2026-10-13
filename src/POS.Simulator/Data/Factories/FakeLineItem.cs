using Bogus;
using POS.Simulator.@class;

namespace POS.Simulator.Data.Factories;

public sealed class FakeFactoryPosLineItem : Faker<PosLineItems>
{
    private int lineNo;
    private int quantity;
    private double unitPrice;
    private double lineAmount;
    private bool vatable;
    private readonly CustomerType customerType;

    public FakeFactoryPosLineItem(CustomerType customerType)
    {
        this.customerType = customerType;

        RuleFor(p => p.LineNo, _ => ++lineNo);
        RuleFor(p => p.Sku, faker => faker.Commerce.ProductName());
        RuleFor(p => p.Description, faker => faker.Lorem.Sentence());

        RuleFor(p => p.Quantity, faker => quantity = faker.Random.Int(1, 10));

        RuleFor(p => p.UnitPrice, faker =>
            unitPrice = (double)faker.Finance.Amount(20m, 4000m, 2));

        RuleFor(p => p.LineAmount, _ => lineAmount = quantity * unitPrice);

        RuleFor(p => p.Vatable, faker => vatable = faker.Random.Bool());

        // Must run AFTER Vatable's rule — it reads the field that rule just set.
        RuleFor(p => p.TaxType, faker =>
        {
            if (vatable)
                return null; // a vatable line never carries a TaxType

            var allowed = new List<taxType> { taxType.VAT_EXEMPT };

            if (customerType == CustomerType.REGISTERED_BUSSINESS)
                allowed.Add(taxType.ZERO_RATED);

            if (customerType is CustomerType.SENIOR_CITIZEN or CustomerType.PWD)
                allowed.Add(taxType.VAT_EXEMPT_SCPWD);

            return faker.PickRandom(allowed);
        });
    }
}