namespace MyHomeSolution.Application.Common.Interfaces;

public interface IJwtTokenService
{
    Task<TokenResult> GenerateTokensAsync(
        string userId, CancellationToken cancellationToken = default);

    Task<TokenResult?> RefreshTokensAsync(
        string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeRefreshTokensAsync(
        string userId, CancellationToken cancellationToken = default);
}

public sealed record TokenResult(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken);
