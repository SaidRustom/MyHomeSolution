using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Infrastructure.Persistence;

namespace MyHomeSolution.Api.Middleware;

/// <summary>
/// Middleware that increments the action count for demo users on every
/// mutating API request (POST, PUT, PATCH, DELETE) and GET requests.
/// Uses fire-and-forget to avoid adding latency to the request pipeline.
/// </summary>
public sealed class DemoActionTrackingMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> TrackedMethods =
        ["GET", "POST", "PUT", "PATCH", "DELETE"];

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Only track for authenticated users on API endpoints
        if (!context.User.Identity?.IsAuthenticated ?? true)
            return;

        if (!context.Request.Path.StartsWithSegments("/api"))
            return;

        var method = context.Request.Method.ToUpperInvariant();
        if (!TrackedMethods.Contains(method))
            return;

        // Skip tracking on the demo status endpoint itself to avoid infinite loops
        if (context.Request.Path.StartsWithSegments("/api/demo"))
            return;

        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? context.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return;

        // Fire and forget — don't slow down the response
        _ = IncrementActionCountAsync(context.RequestServices, userId);
    }

    private static async Task IncrementActionCountAsync(
        IServiceProvider services, string userId)
    {
        try
        {
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.DemoUsers
                .Where(d => d.UserId == userId && d.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(
                    d => d.ActionCount,
                    d => d.ActionCount + 1));
        }
        catch
        {
            // Non-critical: swallow errors to avoid breaking the request pipeline
        }
    }
}
