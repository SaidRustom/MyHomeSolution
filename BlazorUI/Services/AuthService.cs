using System.Net.Http.Json;
using System.Text.Json;
using BlazorUI.Infrastructure.Auth;
using BlazorUI.Infrastructure.LocalStorage;
using BlazorUI.Models.Auth;
using BlazorUI.Models.Common;
using BlazorUI.Services.Contracts;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorUI.Services;

public sealed class AuthService(
    HttpClient httpClient,
    IStorageManager storage,
    AuthenticationStateProvider authStateProvider) : IAuthService
{
    private const string BasePath = "api/auth";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ApiResult<AuthResponse>> LoginAsync(
        LoginRequest request, CancellationToken cancellationToken = default)
    {
        var body = new { request.Email, request.Password };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/login", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            var problem = await TryReadProblemAsync(response, cancellationToken);
            return ApiResult<AuthResponse>.Failure(problem, statusCode);
        }

        var authResponse = await response.Content
            .ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken);

        if (authResponse is null)
        {
            return ApiResult<AuthResponse>.Failure(
                new ApiProblemDetails { Title = "Invalid response", Detail = "The server returned an unexpected response." },
                statusCode);
        }

        await PersistTokensAsync(authResponse);
        NotifyAuthStateChanged();

        return ApiResult<AuthResponse>.Success(authResponse, statusCode);
    }

    public async Task<ApiResult> RegisterAsync(
        RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.IsDemoUser
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/register", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
            return ApiResult.Success(statusCode);

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult.Failure(problem, statusCode);
    }

    public async Task<ApiResult<AuthResponse>> RefreshTokenAsync(
        CancellationToken cancellationToken = default)
    {
        var tokens = await storage.GetAsync<AuthTokens>(AuthConstants.TokenStorageKey);

        if (tokens is null)
        {
            return ApiResult<AuthResponse>.Failure(
                new ApiProblemDetails { Title = "Not authenticated", Detail = "No refresh token available." },
                401);
        }

        var body = new { tokens.RefreshToken };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/refresh", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (!response.IsSuccessStatusCode)
        {
            await LogoutAsync();
            var problem = await TryReadProblemAsync(response, cancellationToken);
            return ApiResult<AuthResponse>.Failure(problem, statusCode);
        }

        var authResponse = await response.Content
            .ReadFromJsonAsync<AuthResponse>(JsonOptions, cancellationToken);

        if (authResponse is null)
        {
            await LogoutAsync();
            return ApiResult<AuthResponse>.Failure(
                new ApiProblemDetails { Title = "Invalid response", Detail = "Failed to refresh token." },
                statusCode);
        }

        await PersistTokensAsync(authResponse);
        NotifyAuthStateChanged();

        return ApiResult<AuthResponse>.Success(authResponse, statusCode);
    }

    public async Task LogoutAsync()
    {
        await storage.RemoveAsync(AuthConstants.TokenStorageKey);
        NotifyAuthStateChanged();
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var tokens = await storage.GetAsync<AuthTokens>(AuthConstants.TokenStorageKey);
        return tokens is not null && tokens.ExpiresAt > DateTimeOffset.UtcNow;
    }

    public async Task<ApiResult> ConfirmEmailAsync(
        string userId, string token, CancellationToken cancellationToken = default)
    {
        var body = new { userId, token };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/confirm-email", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
            return ApiResult.Success(statusCode);

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult.Failure(problem, statusCode);
    }

    public async Task<ApiResult> ForgotPasswordAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var body = new { email };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/forgot-password", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
            return ApiResult.Success(statusCode);

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult.Failure(problem, statusCode);
    }

    public async Task<ApiResult> ResetPasswordAsync(
        ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var body = new { request.UserId, request.Token, request.NewPassword };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/reset-password", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
            return ApiResult.Success(statusCode);

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult.Failure(problem, statusCode);
    }

    public async Task<ApiResult> ResendConfirmationAsync(
        string email, CancellationToken cancellationToken = default)
    {
        var body = new { email };

        using var response = await httpClient.PostAsJsonAsync(
            $"{BasePath}/resend-confirmation", body, JsonOptions, cancellationToken);

        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
            return ApiResult.Success(statusCode);

        var problem = await TryReadProblemAsync(response, cancellationToken);
        return ApiResult.Failure(problem, statusCode);
    }

    private async Task PersistTokensAsync(AuthResponse response)
    {
        var tokens = new AuthTokens
        {
            AccessToken = response.AccessToken,
            RefreshToken = response.RefreshToken,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(response.ExpiresIn)
        };

        await storage.SetAsync(AuthConstants.TokenStorageKey, tokens, TimeSpan.FromDays(30));
    }

    private void NotifyAuthStateChanged()
    {
        if (authStateProvider is JwtAuthenticationStateProvider jwt)
        {
            jwt.NotifyAuthenticationStateChanged();
        }
    }

    private static async Task<ApiProblemDetails> TryReadProblemAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var problem = await response.Content
                .ReadFromJsonAsync<ApiProblemDetails>(JsonOptions, cancellationToken);

            if (problem is not null)
                return problem;
        }
        catch (JsonException)
        {
        }

        return new ApiProblemDetails
        {
            Title = response.ReasonPhrase ?? "Error",
            Status = (int)response.StatusCode,
            Detail = $"Request failed with status code {(int)response.StatusCode}."
        };
    }
}
