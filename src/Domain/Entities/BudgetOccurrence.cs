using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Represents a single period instance of a budget (e.g., January 2025, Week 12 2025).
/// SpentAmount is computed from linked bills (via BillBudgetLink).
/// </summary>
public sealed class BudgetOccurrence : BaseEntity
{
    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = default!;

    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }

    /// <summary>
    /// The allocated amount for this period. Initially inherited from the budget, but can be edited.
    /// </summary>
    public decimal AllocatedAmount { get; set; }

    /// <summary>
    /// The actual amount spent during this period.
    /// Computed from linked non-deleted bills: BillLinks.Where(!Bill.IsDeleted).Sum(Bill.Amount).
    /// Mapped as a database computed column.
    /// </summary>
    public decimal SpentAmount { get; private set; }

    /// <summary>
    /// Amount carried over from the previous occurrence (rollover).
    /// </summary>
    public decimal CarryoverAmount { get; set; }

    public string? Notes { get; set; }

    public ICollection<BillBudgetLink> BillLinks { get; set; } = [];
    public ICollection<BudgetTransfer> OutgoingTransfers { get; set; } = [];
    public ICollection<BudgetTransfer> IncomingTransfers { get; set; } = [];

    /// <summary>
    /// Computed: AllocatedAmount + CarryoverAmount - SpentAmount.
    /// Mapped as a database computed column.
    /// </summary>
    public decimal Balance { get; private set; }

    /// <summary>
    /// Computed: true when PeriodStart &lt;= GETUTCDATE() AND PeriodEnd >= GETUTCDATE().
    /// Mapped as a database computed column.
    /// </summary>
    public bool IsActive { get; private set; }
}
