using Microsoft.AspNetCore.Mvc;
using TestBackendService.Models;
using TestBackendService.Services;

namespace TestBackendService.Controllers;

[ApiController]
[Route("mock")]
public class MockDataController : ControllerBase
{
    private readonly IMockDataService _service;

    public MockDataController(IMockDataService service)
    {
        _service = service;
    }

    // GET /mock/users?count=10&seed=123
    [HttpGet("users")]
    public ActionResult<IEnumerable<UserDto>> GetUsers([FromQuery] int count = 10, [FromQuery] int? seed = null)
    {
        var users = _service.GetUsers(count, seed);
        return Ok(users);
    }

    // GET /mock/products?count=10&seed=123
    [HttpGet("products")]
    public ActionResult<IEnumerable<ProductDto>> GetProducts([FromQuery] int count = 10, [FromQuery] int? seed = null)
    {
        var products = _service.GetProducts(count, seed);
        return Ok(products);
    }

    // GET /mock/company?seed=123
    [HttpGet("company")]
    public ActionResult<CompanyDto> GetCompany([FromQuery] int? seed = null)
    {
        var company = _service.GetCompany(seed);
        return Ok(company);
    }
}
