using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public AuditLog() { }

    public AuditLog(
        string entityName,
        string entityId,
        string? userId,
        AuditActionType actionType,
        DateTimeOffset timestamp)
    {
        EntityName = entityName;
        EntityId = entityId;
        UserId = userId;
        ActionType = actionType;
        Timestamp = timestamp;
    }

    public string EntityName { get; init; } = default!;
    public string EntityId { get; init; } = default!;
    public AuditActionType ActionType { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? UserId { get; init; }

    public ICollection<AuditHistoryEntry> HistoryEntries { get; set; } = [];
}
