using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class HouseholdTask : BaseAuditableEntity, IBillable
{
    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? DueDate { get; set; }
    public string? AssignedToUserId { get; set; }

    public RecurrencePattern? RecurrencePattern { get; set; }
    public ICollection<TaskOccurrence> Occurrences { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
