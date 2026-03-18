using System.ComponentModel.DataAnnotations;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class HouseholdTask : BaseAuditableEntity, IBillable
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public string Title { get; set; } = default!;
    public string? Description { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskCategory Category { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsRecurring { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly? DueDate { get; set; }
    public string? AssignedToUserId { get; set; }

    // Auto-bill configuration: when enabled, a Bill is created for each occurrence
    public bool AutoCreateBill { get; set; }
    public decimal? DefaultBillAmount { get; set; }
    public string? DefaultBillCurrency { get; set; }
    public BillCategory? DefaultBillCategory { get; set; }
    public string? DefaultBillTitle { get; set; }
    public string? DefaultBillPaidByUserId { get; set; }

    /// <summary>
    /// Default budget to attach when a bill is auto-created from this task.
    /// </summary>
    public Guid? DefaultBudgetId { get; set; }
    public Budget? DefaultBudget { get; set; }

    public RecurrencePattern? RecurrencePattern { get; set; }
    public ICollection<TaskOccurrence> Occurrences { get; set; } = [];
    public ICollection<Bill> Bills { get; set; } = [];
}
