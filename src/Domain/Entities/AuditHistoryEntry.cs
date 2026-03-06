using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public sealed class AuditHistoryEntry : BaseEntity
{
    public AuditHistoryEntry() { }

    public AuditHistoryEntry(AuditLog auditLog)
    {
        AuditLogId = auditLog.Id;
    }

    public Guid AuditLogId { get; init; }
    public string PropertyName { get; init; } = default!;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }

    public AuditLog AuditLog { get; init; } = default!;
}
