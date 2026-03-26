namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Tracks demo user registrations. Persists even after the demo user
/// and all their data are purged after the 24-hour window.
/// The same email can register multiple times as long as it is not
/// currently an active (non-demo) account and not currently in an active demo session.
/// </summary>
public sealed class DemoUser
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Identity user ID assigned during demo registration.</summary>
    public string UserId { get; set; } = default!;

    public string Email { get; set; } = default!;
    public string FullName { get; set; } = default!;

    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>When the demo session expires (CreatedAt + 24 h).</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>True while the demo is still active (user not yet purged).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Total CRUD actions performed by this demo user.</summary>
    public long ActionCount { get; set; }
}
