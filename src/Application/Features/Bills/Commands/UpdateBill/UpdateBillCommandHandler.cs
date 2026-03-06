using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Bills.Commands.UpdateBill;

public sealed class UpdateBillCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<UpdateBillCommand>
{
    public async Task Handle(UpdateBillCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .Include(b => b.Splits)
            .FirstOrDefaultAsync(b => b.Id == request.Id && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.Id);

        var oldAmount = bill.Amount;
        bill.Title = request.Title;
        bill.Description = request.Description;
        bill.Amount = request.Amount;
        bill.Currency = request.Currency;
        bill.Category = request.Category;
        bill.BillDate = request.BillDate;
        bill.Notes = request.Notes;

        if (oldAmount != request.Amount)
        {
            foreach (var split in bill.Splits)
            {
                split.Amount = Math.Round(request.Amount * split.Percentage / 100m, 2);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillUpdatedEvent(bill.Id, bill.Title, userId),
            cancellationToken);
    }
}
