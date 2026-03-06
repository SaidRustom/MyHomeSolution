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
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BillId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.BillId);

        var split = await dbContext.BillSplits
            .FirstOrDefaultAsync(s => s.Id == request.SplitId && s.BillId == request.BillId, cancellationToken)
            ?? throw new NotFoundException(nameof(BillSplit), request.SplitId);

        if (split.UserId != userId && bill.PaidByUserId != userId)
            throw new ForbiddenAccessException();

        split.Status = SplitStatus.Paid;
        split.PaidAt = dateTimeProvider.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillSplitPaidEvent(bill.Id, split.Id, bill.Title, split.UserId, split.Amount),
            cancellationToken);
    }
}
