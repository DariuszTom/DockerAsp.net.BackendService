namespace TestBackendService.Models;

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
