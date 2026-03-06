using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.Bills.Queries.GetBillById;

public sealed class GetBillByIdQueryHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IIdentityService identityService)
    : IRequestHandler<GetBillByIdQuery, BillDetailDto>
{
    public async Task<BillDetailDto> Handle(GetBillByIdQuery request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var bill = await dbContext.Bills
            .AsNoTracking()
            .Include(b => b.Splits)
            .Include(b => b.Items)
            .Where(b => !b.IsDeleted)
            .FirstOrDefaultAsync(b => b.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.Id);

        if (bill.CreatedBy != userId && !bill.Splits.Any(s => s.UserId == userId))
            throw new ForbiddenAccessException();

        var allUserIds = bill.Splits.Select(s => s.UserId)
            .Append(bill.PaidByUserId)
            .Distinct();
        var nameMap = await identityService.GetUserFullNamesByIdsAsync(allUserIds, cancellationToken);

        return new BillDetailDto
        {
            Id = bill.Id,
            Title = bill.Title,
            Description = bill.Description,
            Amount = bill.Amount,
            Currency = bill.Currency,
            Category = bill.Category,
            BillDate = bill.BillDate,
            PaidByUserId = bill.PaidByUserId,
            PaidByUserFullName = nameMap.GetValueOrDefault(bill.PaidByUserId),
            ReceiptUrl = bill.ReceiptUrl,
            RelatedEntityId = bill.RelatedEntityId,
            RelatedEntityType = bill.RelatedEntityType,
            Notes = bill.Notes,
            CreatedAt = bill.CreatedAt,
            CreatedBy = bill.CreatedBy == null ? null : nameMap.GetValueOrDefault(bill.CreatedBy),
            LastModifiedAt = bill.LastModifiedAt,
            Splits = bill.Splits.Select(s => new BillSplitDto
            {
                Id = s.Id,
                UserId = s.UserId,
                UserFullName = nameMap.GetValueOrDefault(s.UserId),
                Percentage = s.Percentage,
                Amount = s.Amount,
                Status = s.Status,
                PaidAt = s.PaidAt
            }).ToList(),
            Items = bill.Items.Select(i => new BillItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Price = i.Price,
                Discount = i.Discount
            }).ToList()
        };
    }
}
