using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using BlazorUI.Infrastructure.LocalStorage;
using BlazorUI.Models.Auth;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorUI.Infrastructure.Auth;

public sealed class JwtAuthenticationStateProvider(IStorageManager storage)
    : AuthenticationStateProvider
{
    private static readonly AuthenticationState Anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var tokens = await storage.GetAsync<AuthTokens>(AuthConstants.TokenStorageKey);

        if (tokens is null || string.IsNullOrWhiteSpace(tokens.AccessToken))
            return Anonymous;

        if (tokens.ExpiresAt <= DateTimeOffset.UtcNow)
            return Anonymous;

        var identity = ParseClaimsFromJwt(tokens.AccessToken);
        var user = new ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    public void NotifyAuthenticationStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    private static ClaimsIdentity ParseClaimsFromJwt(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(jwt))
            return new ClaimsIdentity();

        var token = handler.ReadJwtToken(jwt);
        var claims = new List<Claim>(token.Claims);

        // Map the Identity API "role" claim to the standard ClaimTypes.Role
        // so [Authorize(Roles = "...")] works correctly.
        var roleClaims = token.Claims
            .Where(c => c.Type is "role" or ClaimTypes.Role)
            .Select(c => new Claim(ClaimTypes.Role, c.Value))
            .ToList();

        // Remove duplicates and add normalised role claims
        claims.RemoveAll(c => c.Type is "role");
        claims.AddRange(roleClaims);

        return new ClaimsIdentity(claims, AuthConstants.BearerScheme, "name", ClaimTypes.Role);
    }
}
