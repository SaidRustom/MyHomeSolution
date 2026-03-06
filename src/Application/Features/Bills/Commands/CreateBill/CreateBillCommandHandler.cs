using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBill;

public sealed class CreateBillCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IPublisher publisher)
    : IRequestHandler<CreateBillCommand, Guid>
{
    public async Task<Guid> Handle(CreateBillCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = new Bill
        {
            Title = request.Title,
            Description = request.Description,
            Amount = request.Amount,
            Currency = request.Currency,
            Category = request.Category,
            BillDate = request.BillDate,
            PaidByUserId = userId,
            Notes = request.Notes,
            RelatedEntityId = request.RelatedEntityId,
            RelatedEntityType = request.RelatedEntityType
        };

        var hasCustomPercentages = request.Splits.Any(s => s.Percentage.HasValue);
        var equalPercentage = Math.Round(100m / request.Splits.Count, 2);

        foreach (var splitReq in request.Splits)
        {
            var percentage = hasCustomPercentages
                ? splitReq.Percentage!.Value
                : equalPercentage;

            var splitAmount = Math.Round(request.Amount * percentage / 100m, 2);

            bill.Splits.Add(new BillSplit
            {
                BillId = bill.Id,
                UserId = splitReq.UserId,
                Percentage = percentage,
                Amount = splitAmount,
                Status = splitReq.UserId == userId ? SplitStatus.Paid : SplitStatus.Unpaid
            });
        }

        dbContext.Bills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, userId),
            cancellationToken);

        return bill.Id;
    }
}
