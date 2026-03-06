using BlazorUI.Models.Auth;
using BlazorUI.Models.Common;

namespace BlazorUI.Services.Contracts;

public interface IAuthService
{
    Task<ApiResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult<AuthResponse>> RefreshTokenAsync(CancellationToken cancellationToken = default);
    Task LogoutAsync();
    Task<bool> IsAuthenticatedAsync();
    Task<ApiResult> ConfirmEmailAsync(string userId, string token, CancellationToken cancellationToken = default);
    Task<ApiResult> ForgotPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<ApiResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<ApiResult> ResendConfirmationAsync(string email, CancellationToken cancellationToken = default);
}
