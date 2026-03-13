namespace BlazorUI.Services.Contracts;

public interface IAvatarService
{
    Task<string?> GetAvatarDataUrlAsync(string? userId, CancellationToken cancellationToken = default);

    void InvalidateCache(string userId);
}
