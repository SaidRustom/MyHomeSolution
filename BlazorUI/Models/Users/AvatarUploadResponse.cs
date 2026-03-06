namespace BlazorUI.Models.Users;

public sealed record AvatarUploadResponse
{
    public string AvatarUrl { get; init; } = string.Empty;
}
