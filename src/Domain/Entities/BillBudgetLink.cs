using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Links a Bill to a specific Budget and BudgetOccurrence.
/// A Bill can only be linked to one Budget.
/// </summary>
public sealed class BillBudgetLink : BaseEntity
{
    public Guid BillId { get; set; }
    public Bill Bill { get; set; } = default!;

    public Guid BudgetId { get; set; }
    public Budget Budget { get; set; } = default!;

    public Guid BudgetOccurrenceId { get; set; }
    public BudgetOccurrence BudgetOccurrence { get; set; } = default!;
}
