using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

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

        // Update PaidByUserId if provided
        if (!string.IsNullOrEmpty(request.PaidByUserId))
        {
            bill.PaidByUserId = request.PaidByUserId;
        }

        var splits = new List<BillSplit>();
        // Sync splits if provided
        if (request.Splits is { Count: > 0 })
        {
            // Remove existing splits
            dbContext.BillSplits.RemoveRange(bill.Splits);
            bill.Splits.Clear();

            var hasCustomPercentages = request.Splits.Any(s => s.Percentage.HasValue);
            var equalPercentage = Math.Round(100m / request.Splits.Count, 2);

            foreach (var splitReq in request.Splits)
            {
                var percentage = hasCustomPercentages
                    ? splitReq.Percentage!.Value
                    : equalPercentage;

                var splitAmount = Math.Round(request.Amount * percentage / 100m, 2);

                splits.Add(new BillSplit
                {
                    BillId = bill.Id,
                    UserId = splitReq.UserId,
                    Percentage = percentage,
                    Amount = splitAmount,
                    Status = splitReq.UserId == bill.PaidByUserId ? SplitStatus.Paid : SplitStatus.Unpaid
                });
            }

            dbContext.BillSplits.AddRange(splits);
        }
        else if (oldAmount != request.Amount)
        {
            // Recalculate amounts based on existing percentages
            splits = bill.Splits.ToList();
            foreach (var split in splits)
            {
                split.Amount = Math.Round(request.Amount * split.Percentage / 100m, 2);
                dbContext.BillSplits.Update(split);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillUpdatedEvent(bill.Id, bill.Title, userId),
            cancellationToken);
    }
}
