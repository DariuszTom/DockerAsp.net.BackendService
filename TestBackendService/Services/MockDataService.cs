using Bogus;
using TestBackendService.Models;

namespace TestBackendService.Services;

public sealed class MockDataService : IMockDataService
{
    private static readonly object _seedLock = new();

    public IReadOnlyList<UserDto> GetUsers(int count, int? seed = null)
    {
        count = Math.Clamp(count, 1, 1000);
        var faker = CreateUserFaker(seed);
        return faker.Generate(count);
    }

    public IReadOnlyList<ProductDto> GetProducts(int count, int? seed = null)
    {
        count = Math.Clamp(count, 1, 1000);
        var faker = CreateProductFaker(seed);
        return faker.Generate(count);
    }

    public CompanyDto GetCompany(int? seed = null)
    {
        ApplySeed(seed);
        var faker = new Faker("en");
        return new CompanyDto(
            Name: faker.Company.CompanyName(),
            CatchPhrase: faker.Company.CatchPhrase(),
            Bs: faker.Company.Bs(),
            Phone: faker.Phone.PhoneNumber(),
            Address: new AddressDto(
                Street: faker.Address.StreetAddress(),
                City: faker.Address.City(),
                State: faker.Address.State(),
                PostalCode: faker.Address.ZipCode(),
                Country: faker.Address.Country()
            )
        );
    }

    private static Faker<UserDto> CreateUserFaker(int? seed)
    {
        ApplySeed(seed);
        return new Faker<UserDto>(locale: "en")
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.BirthDate, f => f.Date.Past(50, DateTime.UtcNow.AddYears(-18)))
            .RuleFor(u => u.Address, f => new AddressDto(
                Street: f.Address.StreetAddress(),
                City: f.Address.City(),
                State: f.Address.State(),
                PostalCode: f.Address.ZipCode(),
                Country: f.Address.Country()
            ));
    }

    private static Faker<ProductDto> CreateProductFaker(int? seed)
    {
        ApplySeed(seed);
        return new Faker<ProductDto>(locale: "en")
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Sku, f => f.Commerce.Ean13())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.Price, f => f.Finance.Amount(1, 999, 2))
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Color, f => f.Commerce.Color());
    }

    private static void ApplySeed(int? seed)
    {
        if (seed.HasValue)
        {
            lock (_seedLock)
            {
                Randomizer.Seed = new Random(seed.Value);
            }
        }
    }
}
