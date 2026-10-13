using Bogus;
using POS.Simulator.@class;

namespace POS.Simulator.Data.Factories;


public sealed class FakeFactoryPosLineItem : Faker<PosLineItems>
{
    private int lineNo;
    private int quantity;
    private double unitPrice;
    private double lineAmount;
    public FakeFactoryPosLineItem()
    {
        lineAmount = quantity * unitPrice;
        RuleFor(p => p.LineNo, _ => ++lineNo);
        RuleFor(p => p.Sku, faker => faker.Commerce.ProductName());
        RuleFor(p => p.Description, faker => faker.Lorem.Sentence());
        RuleFor(p => p.Quantity, faker => 
        {
            quantity = faker.Random.Int(1, 10);
            return quantity;
        });
        RuleFor(p => p.UnitPrice, faker => 
        {
            unitPrice = (double)faker.Finance.Amount(20m, 4000m, 2);
            return unitPrice;
        });
        RuleFor(p => p.LineAmount, _ => lineAmount = quantity * unitPrice);
        RuleFor(p => p.Vatable, faker => faker.Random.Bool());
    }
}
