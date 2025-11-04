using Bogus;
using Microsoft.AspNetCore.Mvc;

namespace TestBackendService.Controllers;

[ApiController]
[Route("mock")]
public class MockDataController : ControllerBase
{
    // GET /mock/users?count=10&seed=123
    [HttpGet("users")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int count = 10, [FromQuery] int? seed = null)
    {
        count = Math.Clamp(count, 1, 1000);

        var faker = CreateUserFaker(seed);
        var users = faker.Generate(count);
        return Ok(users);
    }

    // GET /mock/products?count=10&seed=123
    [HttpGet("products")]
    public ActionResult<IEnumerable<ProductDto>> GetProducts([FromQuery] int count = 10, [FromQuery] int? seed = null)
    {
        count = Math.Clamp(count, 1, 1000);

        var faker = CreateProductFaker(seed);
        var products = faker.Generate(count);
        return Ok(products);
    }

    // GET /mock/company?seed=123
    [HttpGet("company")]
    public ActionResult<CompanyDto> GetCompany([FromQuery] int? seed = null)
    {
        if (seed.HasValue)
        {
            Randomizer.Seed = new Random(seed.Value);
        }

        var faker = new Faker("en");

        var company = new CompanyDto
        (
            Name: faker.Company.CompanyName(),
            CatchPhrase: faker.Company.CatchPhrase(),
            Bs: faker.Company.Bs(),
            Phone: faker.Phone.PhoneNumber(),
            Address: new AddressDto
            (
                Street: faker.Address.StreetAddress(),
                City: faker.Address.City(),
                State: faker.Address.State(),
                PostalCode: faker.Address.ZipCode(),
                Country: faker.Address.Country()
            )
        );

        return Ok(company);
    }

    private static Faker<UserDto> CreateUserFaker(int? seed)
    {
        if (seed.HasValue)
        {
            Randomizer.Seed = new Random(seed.Value);
        }

        var faker = new Faker<UserDto>(locale: "en");

        return faker
            .RuleFor(u => u.Id, f => f.Random.Guid())
            .RuleFor(u => u.FirstName, f => f.Name.FirstName())
            .RuleFor(u => u.LastName, f => f.Name.LastName())
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.FirstName, u.LastName))
            .RuleFor(u => u.Phone, f => f.Phone.PhoneNumber())
            .RuleFor(u => u.BirthDate, f => f.Date.Past(50, DateTime.UtcNow.AddYears(-18)))
            .RuleFor(u => u.Address, f => new AddressDto
            (
                Street: f.Address.StreetAddress(),
                City: f.Address.City(),
                State: f.Address.State(),
                PostalCode: f.Address.ZipCode(),
                Country: f.Address.Country()
            ));
    }

    private static Faker<ProductDto> CreateProductFaker(int? seed)
    {
        if (seed.HasValue)
        {
            Randomizer.Seed = new Random(seed.Value);
        }

        var faker = new Faker<ProductDto>(locale: "en");

        return faker
            .RuleFor(p => p.Id, f => f.Random.Guid())
            .RuleFor(p => p.Sku, f => f.Commerce.Ean13())
            .RuleFor(p => p.Name, f => f.Commerce.ProductName())
            .RuleFor(p => p.Category, f => f.Commerce.Categories(1)[0])
            .RuleFor(p => p.Price, f => f.Finance.Amount(1, 999, 2))
            .RuleFor(p => p.Description, f => f.Commerce.ProductDescription())
            .RuleFor(p => p.Color, f => f.Commerce.Color());
    }
}

public record AddressDto(string Street, string City, string State, string PostalCode, string Country);

public record UserDto
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public DateTime BirthDate { get; init; }
    public AddressDto Address { get; init; } = new("", "", "", "", "");
}

public record ProductDto
{
    public Guid Id { get; init; }
    public string Sku { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Category { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Color { get; init; } = string.Empty;
}

public record CompanyDto(string Name, string CatchPhrase, string Bs, string Phone, AddressDto Address);
