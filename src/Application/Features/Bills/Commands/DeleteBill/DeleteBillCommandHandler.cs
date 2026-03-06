using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Bills.Commands.DeleteBill;

public sealed class DeleteBillCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<DeleteBillCommand>
{
    public async Task Handle(DeleteBillCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.Id);

        var affectedUserIds = bill.Splits
            .Where(s => s.UserId != userId)
            .Select(s => s.UserId)
            .Distinct()
            .ToList();

        bill.IsDeleted = true;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillDeletedEvent(bill.Id, bill.Title, userId, affectedUserIds),
            cancellationToken);
    }
}
