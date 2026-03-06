using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class EntityShare : BaseAuditableEntity
{
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = default!;
    public string SharedWithUserId { get; set; } = default!;
    public SharePermission Permission { get; set; }
}
