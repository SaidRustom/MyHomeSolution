using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Commands.TransferBudgetFunds;

public sealed class TransferBudgetFundsCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<TransferBudgetFundsCommand, Guid>
{
    public async Task<Guid> Handle(TransferBudgetFundsCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var source = await dbContext.BudgetOccurrences
            .Include(o => o.Budget)
            .FirstOrDefaultAsync(o => o.Id == request.SourceOccurrenceId, cancellationToken)
            ?? throw new NotFoundException(nameof(BudgetOccurrence), request.SourceOccurrenceId);

        var destination = await dbContext.BudgetOccurrences
            .Include(o => o.Budget)
            .FirstOrDefaultAsync(o => o.Id == request.DestinationOccurrenceId, cancellationToken)
            ?? throw new NotFoundException(nameof(BudgetOccurrence), request.DestinationOccurrenceId);

        // Deduct from source, add to destination
        source.AllocatedAmount -= request.Amount;
        destination.AllocatedAmount += request.Amount;

        var transfer = new BudgetTransfer
        {
            SourceOccurrenceId = source.Id,
            DestinationOccurrenceId = destination.Id,
            Amount = request.Amount,
            Reason = request.Reason
        };

        dbContext.BudgetTransfers.Add(transfer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return transfer.Id;
    }
}
