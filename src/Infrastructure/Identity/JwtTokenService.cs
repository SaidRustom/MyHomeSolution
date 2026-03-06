using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Infrastructure.Configuration;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Infrastructure.Identity;

public sealed class JwtTokenService(
    UserManager<ApplicationUser> userManager,
    ApplicationDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    IDateTimeProvider dateTimeProvider) : IJwtTokenService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<TokenResult> GenerateTokensAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new InvalidOperationException($"User '{userId}' not found.");

        var accessToken = await CreateAccessTokenAsync(user);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        var expiresIn = _jwt.AccessTokenExpirationMinutes * 60;

        return new TokenResult(accessToken, expiresIn, refreshToken);
    }

    public async Task<TokenResult?> RefreshTokensAsync(
        string refreshToken, CancellationToken cancellationToken = default)
    {
        var stored = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken, cancellationToken);

        if (stored is null || !stored.IsActive)
            return null;

        // Revoke the used token (rotation)
        stored.RevokedAt = dateTimeProvider.UtcNow;

        var user = await userManager.FindByIdAsync(stored.UserId);
        if (user is null || !user.IsActive)
            return null;

        var newAccessToken = await CreateAccessTokenAsync(user);
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id, cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);

        var expiresIn = _jwt.AccessTokenExpirationMinutes * 60;

        return new TokenResult(newAccessToken, expiresIn, newRefreshToken);
    }

    public async Task RevokeRefreshTokensAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        var now = dateTimeProvider.UtcNow;

        foreach (var token in activeTokens)
        {
            token.RevokedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> CreateAccessTokenAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email!),
            new(ClaimTypes.Name, user.FullName),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("name", user.FullName),
            new("email", user.Email!)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("role", role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpirationMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(
        string userId, CancellationToken cancellationToken)
    {
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Token = tokenValue,
            CreatedAt = dateTimeProvider.UtcNow,
            ExpiresAt = dateTimeProvider.UtcNow.AddDays(_jwt.RefreshTokenExpirationDays)
        };

        dbContext.RefreshTokens.Add(refreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return tokenValue;
    }
}
