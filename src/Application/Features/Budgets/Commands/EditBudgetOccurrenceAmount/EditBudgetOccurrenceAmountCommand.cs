using MediatR;

namespace MyHomeSolution.Application.Features.Budgets.Commands.EditBudgetOccurrenceAmount;

public sealed record EditBudgetOccurrenceAmountCommand : IRequest
{
    public Guid OccurrenceId { get; init; }
    public decimal NewAmount { get; init; }
    public string? Notes { get; init; }

    /// <summary>
    /// Optional: another occurrence to transfer the difference from/to.
    /// </summary>
    public Guid? TransferOccurrenceId { get; init; }

    /// <summary>
    /// Reason for the transfer (if TransferOccurrenceId is specified).
    /// </summary>
    public string? TransferReason { get; init; }
}
