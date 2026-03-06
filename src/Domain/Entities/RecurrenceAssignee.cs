using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

public sealed class RecurrenceAssignee : BaseEntity
{
    public Guid RecurrencePatternId { get; set; }
    public string UserId { get; set; } = default!;
    public int Order { get; set; }

    public RecurrencePattern RecurrencePattern { get; set; } = default!;
}
