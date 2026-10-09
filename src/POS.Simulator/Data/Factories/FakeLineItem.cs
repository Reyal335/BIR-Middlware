using Bogus;
using POS.Simulator.@class;

namespace POS.Simulator.Data.Factories;


public sealed class FakeFactoryPosLineItem : Faker<PosLineItems>
{
    public Faker fake;
    private int lineNo;
    private int quantity;
    private double unitPrice;
    private double lineAmount;
    public FakeFactoryPosLineItem()
    {
        fake = new Faker();
        quantity = fake.Random.Int(1, 10);
        unitPrice = (double)fake.Finance.Amount(20m, 4000m, 2);
        lineAmount = quantity * unitPrice;
        RuleFor(p => p.LineNo, _ => ++lineNo);
        RuleFor(p => p.Sku, faker => faker.Commerce.ProductName());
        RuleFor(p => p.Description, faker => faker.Lorem.Sentence());
        RuleFor(p => p.Quantity, _ => quantity);
        RuleFor(p => p.UnitPrice, _ => unitPrice);
        RuleFor(p => p.LineAmount, _ => lineAmount);
        RuleFor(p => p.Vatable, faker => faker.Random.Bool());
    }
}
