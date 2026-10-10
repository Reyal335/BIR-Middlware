using Bogus;
using POS.Simulator.@class;

namespace POS.Simulator.Data.Factories;

public enum CustomerType
{
    WALK_IN,
    REGISTERED
}

public sealed class FakeCustomer : Faker<Customer>
{
    public FakeCustomer()
    {
        RuleFor(c => c.Type, faker => faker.PickRandom<CustomerType>().ToString());
        RuleFor(c => c.RegisteredName, faker => faker.Name.FullName());
        RuleFor(c => c.Tin, faker => faker.Random.Int(100000000, 999999999).ToString());
        RuleFor(c => c.Address, faker => faker.Address.FullAddress());
    }
}