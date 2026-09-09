namespace Khidma.Api.Contracts.Auth;

public sealed class CurrentUserDto
{
    public required string Id { get; init; }

    public required string Email { get; init; }

    public required string FullName { get; init; }

    public required string Role { get; init; }
}
