namespace BlazorUI.Models.Auth;

/// <summary>
/// Maps to the response from the ASP.NET Core Identity <c>/login</c> endpoint
/// when <c>useCookies=false</c>.
/// </summary>
public sealed record AuthResponse
{
    public string TokenType { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public int ExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
}
