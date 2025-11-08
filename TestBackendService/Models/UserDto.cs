namespace TestBackendService.Models;

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
