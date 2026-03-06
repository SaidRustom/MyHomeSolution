using System.Net;
using System.Net.Http.Headers;
using BlazorUI.Infrastructure.LocalStorage;
using BlazorUI.Models.Auth;
using BlazorUI.Services.Contracts;

namespace BlazorUI.Infrastructure.Auth;

/// <summary>
/// Delegating handler that attaches the stored JWT bearer token to every
/// outgoing API request and transparently refreshes expired tokens.
/// </summary>
public sealed class AuthTokenHandler(
    IStorageManager storage,
    IServiceProvider serviceProvider) : DelegatingHandler
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        await AttachTokenAsync(request, cancellationToken);
        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode is HttpStatusCode.Unauthorized)
        {
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            if (refreshed)
            {
                await AttachTokenAsync(request, cancellationToken);
                response = await base.SendAsync(request, cancellationToken);
            }
        }

        return response;
    }

    private async Task AttachTokenAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var tokens = await storage.GetAsync<AuthTokens>(AuthConstants.TokenStorageKey);

        if (tokens is not null && !string.IsNullOrWhiteSpace(tokens.AccessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(AuthConstants.BearerScheme, tokens.AccessToken);
        }
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            // Resolve IAuthService lazily to avoid circular DI
            var authService = serviceProvider.GetRequiredService<IAuthService>();
            var result = await authService.RefreshTokenAsync(cancellationToken);
            return result.IsSuccess;
        }
        catch
        {
            return false;
        }
        finally
        {
            RefreshLock.Release();
        }
    }
}
