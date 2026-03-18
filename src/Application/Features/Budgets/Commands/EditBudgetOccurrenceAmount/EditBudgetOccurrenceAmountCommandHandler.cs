using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Budgets.Commands.EditBudgetOccurrenceAmount;

public sealed class EditBudgetOccurrenceAmountCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<EditBudgetOccurrenceAmountCommand>
{
    public async Task Handle(EditBudgetOccurrenceAmountCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var occurrence = await dbContext.BudgetOccurrences
            .Include(o => o.Budget)
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId, cancellationToken)
            ?? throw new NotFoundException(nameof(BudgetOccurrence), request.OccurrenceId);

        var oldAmount = occurrence.AllocatedAmount;
        var difference = request.NewAmount - oldAmount;

        occurrence.AllocatedAmount = request.NewAmount;

        if (!string.IsNullOrEmpty(request.Notes))
            occurrence.Notes = request.Notes;

        // If a transfer occurrence is specified, create a transfer record
        if (request.TransferOccurrenceId.HasValue && difference != 0)
        {
            var transferOccurrence = await dbContext.BudgetOccurrences
                .FirstOrDefaultAsync(o => o.Id == request.TransferOccurrenceId.Value, cancellationToken)
                ?? throw new NotFoundException(nameof(BudgetOccurrence), request.TransferOccurrenceId.Value);

            if (difference > 0)
            {
                // Increasing this occurrence: transfer FROM the other occurrence
                transferOccurrence.AllocatedAmount -= difference;

                dbContext.BudgetTransfers.Add(new BudgetTransfer
                {
                    SourceOccurrenceId = transferOccurrence.Id,
                    DestinationOccurrenceId = occurrence.Id,
                    Amount = difference,
                    Reason = request.TransferReason ?? $"Budget allocation adjustment"
                });
            }
            else
            {
                // Decreasing this occurrence: transfer TO the other occurrence
                var positiveAmount = Math.Abs(difference);
                transferOccurrence.AllocatedAmount += positiveAmount;

                dbContext.BudgetTransfers.Add(new BudgetTransfer
                {
                    SourceOccurrenceId = occurrence.Id,
                    DestinationOccurrenceId = transferOccurrence.Id,
                    Amount = positiveAmount,
                    Reason = request.TransferReason ?? $"Budget allocation adjustment"
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
