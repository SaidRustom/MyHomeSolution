using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.MarkSplitAsPaid;

public sealed class MarkSplitAsPaidCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<MarkSplitAsPaidCommand>
{
    public async Task Handle(MarkSplitAsPaidCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == request.BillId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.BillId);

        var split = bill.Splits.FirstOrDefault(s => s.Id == request.SplitId)
            ?? throw new NotFoundException(nameof(BillSplit), request.SplitId);

        if (split.UserId != userId && bill.PaidByUserId != userId)
            throw new ForbiddenAccessException();

        var now = dateTimeProvider.UtcNow;

        // The user who initiates payment is the payer.
        // Mark the entire bill as fully paid:
        // - The payer's own split is marked Paid (they paid their share directly).
        // - All other splits are marked Paid with OwedToUserId = payer,
        //   meaning those users now owe the payer their share.
        var payerUserId = userId;

        foreach (var s in bill.Splits)
        {
            if (s.Status == SplitStatus.Paid)
                continue;

            s.Status = SplitStatus.Paid;
            s.PaidAt = now;

            if (!string.Equals(s.UserId, payerUserId, StringComparison.OrdinalIgnoreCase))
            {
                s.OwedToUserId = payerUserId;
            }
        }

        // Update the bill's PaidByUserId to reflect who actually paid
        bill.PaidByUserId = payerUserId;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillSplitPaidEvent(bill.Id, split.Id, bill.Title, payerUserId, bill.Amount),
            cancellationToken);
    }
}
