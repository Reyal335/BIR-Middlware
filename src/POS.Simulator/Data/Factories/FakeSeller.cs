using Bogus;
using POS.Simulator.@class;

namespace POS.Simulator.Data.Factories;

public enum vatRegistration
{
    VAT,
    NON_VAT
}

public sealed class FakeFactorySeller : Faker<Seller>
{
    public FakeFactorySeller()
    {
        RuleFor(p => p.RegisteredName, faker => faker.Company.CompanyName());
        RuleFor(p => p.TradeName, faker => faker.Company.CompanySuffix());
        RuleFor(p => p.Tin, faker => faker.Random.Int(100000000, 999999999).ToString());
        RuleFor(p => p.BranchCode, faker => faker.Random.Int(10000, 9999).ToString());
        RuleFor(p => p.Address, faker => faker.Address.FullAddress());
        RuleFor(p => p.VatRegistration, faker => {
            vatRegistration val = faker.PickRandom<vatRegistration>();
            return val.ToString();
        });
        RuleFor(p => p.PtuOrAccerditationNo, faker => faker.Random.Replace("###-###-####-#####"));
        RuleFor(p => p.MachineAccreditationNo, faker => faker.Random.Replace("###-########"));
    }
}