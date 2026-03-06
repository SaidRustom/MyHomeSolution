namespace BlazorUI.Models.Users;

public sealed record UpdateUserProfileRequest
{
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required string Email { get; init; }
    public string? AvatarUrl { get; init; }
}
