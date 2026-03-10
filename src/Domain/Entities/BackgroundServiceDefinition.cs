using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Represents a registered background service in the system.
/// Seeded at startup — one row per hosted service.
/// </summary>
public sealed class BackgroundServiceDefinition : BaseEntity
{
    public string Name { get; set; } = default!;

    public string Description { get; set; } = default!;

    /// <summary>Assembly-qualified type name for identification.</summary>
    public string QualifiedTypeName { get; set; } = default!;

    public bool IsEnabled { get; set; } = true;

    public DateTimeOffset RegisteredAt { get; set; }

    public ICollection<BackgroundServiceLog> Logs { get; set; } = [];
}
