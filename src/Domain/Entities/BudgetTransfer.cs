using MyHomeSolution.Domain.Common;

namespace MyHomeSolution.Domain.Entities;

/// <summary>
/// Represents a fund transfer between two budget occurrences.
/// </summary>
public sealed class BudgetTransfer : BaseAuditableEntity
{
    public Guid SourceOccurrenceId { get; set; }
    public BudgetOccurrence SourceOccurrence { get; set; } = default!;

    public Guid DestinationOccurrenceId { get; set; }
    public BudgetOccurrence DestinationOccurrence { get; set; } = default!;

    public decimal Amount { get; set; }
    public string? Reason { get; set; }
}
