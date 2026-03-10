using System.ComponentModel.DataAnnotations;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class RecurrencePattern : BaseEntity
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

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

    /// <summary>
    /// Enumerates the due dates this pattern produces, starting from
    /// <paramref name="from"/> (inclusive) up to an optional <paramref name="until"/>
    /// (inclusive) with a hard cap of <paramref name="maxCount"/>.
    /// </summary>
    public IReadOnlyList<DateOnly> EnumerateDueDates(DateOnly from, DateOnly? until, int maxCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxCount);

        var effectiveEnd = until ?? EndDate;
        var dates = new List<DateOnly>(Math.Min(maxCount, 64));
        var current = from < StartDate ? StartDate : from;

        while (dates.Count < maxCount)
        {
            if (effectiveEnd.HasValue && current > effectiveEnd.Value)
                break;

            dates.Add(current);
            current = GetNextOccurrenceDate(current);
        }

        return dates;
    }
}
