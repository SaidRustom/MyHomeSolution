using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.AuditLogs.Common;

public sealed record AuditLogDto
{
    public Guid Id { get; init; }
    public string EntityName { get; init; } = default!;
    public string EntityId { get; init; } = default!;
    public AuditActionType ActionType { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string? UserId { get; init; }
    public string? UserFullName { get; init; }
    public IReadOnlyList<AuditHistoryEntryDto> Changes { get; init; } = [];
}

public sealed record AuditHistoryEntryDto
{
    public Guid Id { get; init; }
    public string PropertyName { get; init; } = default!;
    public string? OldValue { get; init; }
    public string? NewValue { get; init; }
}
