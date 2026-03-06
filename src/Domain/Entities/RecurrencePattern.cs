using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class RecurrencePattern : BaseEntity
{
    public Guid HouseholdTaskId { get; set; }
    public RecurrenceType Type { get; set; }
    public int Interval { get; set; } = 1;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public int LastAssigneeIndex { get; set; } = -1;

    public HouseholdTask HouseholdTask { get; set; } = default!;
    public ICollection<RecurrenceAssignee> Assignees { get; set; } = [];

    public DateOnly GetNextOccurrenceDate(DateOnly fromDate)
    {
        return Type switch
        {
            RecurrenceType.Daily => fromDate.AddDays(Interval),
            RecurrenceType.Weekly => fromDate.AddDays(7 * Interval),
            RecurrenceType.Monthly => fromDate.AddMonths(Interval),
            RecurrenceType.Yearly => fromDate.AddYears(Interval),
            _ => throw new InvalidOperationException($"Unknown recurrence type: {Type}")
        };
    }

    public string? GetNextAssigneeUserId()
    {
        if (Assignees.Count == 0)
            return null;

        var ordered = Assignees.OrderBy(a => a.Order).ToList();
        var nextIndex = (LastAssigneeIndex + 1) % ordered.Count;
        return ordered[nextIndex].UserId;
    }

    public void AdvanceAssigneeIndex()
    {
        if (Assignees.Count == 0)
            return;

        LastAssigneeIndex = (LastAssigneeIndex + 1) % Assignees.Count;
    }
}
