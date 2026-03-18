using System.ComponentModel.DataAnnotations;
using MyHomeSolution.Domain.Common;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Domain.Entities;

public sealed class Budget : BaseAuditableEntity
{
    [Timestamp]
    public byte[] RowVersion { get; set; } = default!;

    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "CAD";
    public BudgetCategory Category { get; set; }
    public BudgetPeriod Period { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public bool IsRecurring { get; set; }

    /// <summary>
    /// Parent budget from which recurring occurrence amounts are transferred.
    /// </summary>
    public Guid? ParentBudgetId { get; set; }
    public Budget? ParentBudget { get; set; }

    public ICollection<Budget> ChildBudgets { get; set; } = [];
    public ICollection<BudgetOccurrence> Occurrences { get; set; } = [];
    public ICollection<BillBudgetLink> BillLinks { get; set; } = [];
}
