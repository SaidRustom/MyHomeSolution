using MediatR;

namespace MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;

public sealed record TransferBudgetFundsCommand : IRequest<Guid>
{
    public Guid SourceOccurrenceId { get; init; }
    public Guid DestinationOccurrenceId { get; init; }
    public decimal Amount { get; init; }
    public string? Reason { get; init; }
}
