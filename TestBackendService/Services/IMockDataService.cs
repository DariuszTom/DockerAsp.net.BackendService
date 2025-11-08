using TestBackendService.Models;

namespace TestBackendService.Services;

public interface IMockDataService
{
    IReadOnlyList<UserDto> GetUsers(int count, int? seed = null);
    IReadOnlyList<ProductDto> GetProducts(int count, int? seed = null);
    CompanyDto GetCompany(int? seed = null);
}
