using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Represents a related entity linked to a bill (many-to-many replacement for
/// the former single RelatedEntityId/RelatedEntityType on Bill).
/// </summary>
public sealed class BillRelatedItem : BaseEntity
{
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = default!;

    public Guid RelatedEntityId { get; set; }
    public string RelatedEntityType { get; set; } = default!;
    public string? RelatedEntityName { get; set; }
}
