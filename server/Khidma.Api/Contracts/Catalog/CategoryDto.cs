namespace Khidma.Api.Contracts.Catalog;

public sealed class CategoryDto
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required bool HasImage { get; init; }
}
