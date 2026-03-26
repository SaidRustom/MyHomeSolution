namespace MyHomeSolution.Application.Common.Interfaces;

public sealed record DemoStatusResult(
    bool IsDemoUser,
    DateTimeOffset ExpiresAt,
    TimeSpan TimeRemaining);

public interface IDemoUserService
{
    /// <summary>Returns the demo status for the given user, or null if not a demo user.</summary>
    Task<DemoStatusResult?> GetDemoStatusAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Seeds all demo data for the user. Called immediately after registration.</summary>
    Task SeedDemoDataAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Increments the action count for the demo user (fire-and-forget safe).</summary>
    Task IncrementActionCountAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Purges all data for expired demo users and sends thank-you emails.</summary>
    Task CleanupExpiredDemoUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>Checks whether the given email is eligible for demo registration.</summary>
    Task<bool> CanRegisterAsDemoAsync(string email, CancellationToken cancellationToken = default);
}
