namespace BlazorUI.Models.Auth;

/// <summary>
/// Persisted token pair stored in local storage.
/// </summary>
public sealed record AuthTokens
{
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTimeOffset ExpiresAt { get; init; }
}
