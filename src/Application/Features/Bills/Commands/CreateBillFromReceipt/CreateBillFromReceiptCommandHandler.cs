using MediatR;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Bills.Commands.CreateBillFromReceipt;

public sealed class CreateBillFromReceiptCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IReceiptAnalysisService receiptAnalysisService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<CreateBillFromReceiptCommand, BillDetailDto>
{
    private const string ContainerName = "receipts";

    public async Task<BillDetailDto> Handle(
        CreateBillFromReceiptCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        // Buffer stream so it can be read twice (analysis + storage)
        using var memoryStream = new MemoryStream();
        await request.Content.CopyToAsync(memoryStream, cancellationToken);

        // 1. Analyze the receipt image
        memoryStream.Position = 0;
        var analysis = await receiptAnalysisService.AnalyzeAsync(
            memoryStream, request.ContentType, cancellationToken);

        // 2. Build description from store address
        var description = !string.IsNullOrWhiteSpace(analysis.StoreAddress)
            ? analysis.StoreAddress
            : null;

        // 3. Build notes with discount info
        string? notes = analysis.Discount > 0
            ? $"Discount applied: {analysis.Discount:F2} {analysis.Currency}"
            : null;

        // 4. Create the bill entity
        var bill = new Bill
        {
            Title = analysis.StoreName,
            Description = description,
            Amount = analysis.Total,
            Currency = analysis.Currency,
            Category = request.Category,
            BillDate = analysis.TransactionDate != default
                ? analysis.TransactionDate
                : dateTimeProvider.UtcNow,
            PaidByUserId = userId,
            Notes = notes
        };

        // 5. Create bill items from analyzed receipt lines
        foreach (var lineItem in analysis.Items)
        {
            var unitPrice = lineItem.Quantity > 0
                ? Math.Round(lineItem.Price / lineItem.Quantity, 2)
                : lineItem.Price;

            bill.Items.Add(new BillItem
            {
                BillId = bill.Id,
                Name = lineItem.Name,
                Quantity = lineItem.Quantity < 1 ? 1 : lineItem.Quantity,
                UnitPrice = unitPrice,
                Price = lineItem.Price,
                Discount = 0m
            });
        }

        // 6. Distribute bill-level discount proportionally across items
        if (analysis.Discount > 0 && bill.Items.Count > 0)
        {
            var itemsTotal = bill.Items.Sum(i => i.Price);
            if (itemsTotal > 0)
            {
                foreach (var item in bill.Items)
                {
                    item.Discount = Math.Round(analysis.Discount * item.Price / itemsTotal, 2);
                }
            }
        }

        // 7. Create splits
        var splits = request.Splits ?? [new BillSplitRequest { UserId = userId }];
        var hasCustomPercentages = splits.Any(s => s.Percentage.HasValue);
        var equalPercentage = Math.Round(100m / splits.Count, 2);

        for (var i = 0; i < splits.Count; i++)
        {
            var splitReq = splits[i];
            var percentage = hasCustomPercentages
                ? splitReq.Percentage!.Value
                : equalPercentage;

            // Absorb rounding remainder into the last split
            if (!hasCustomPercentages && i == splits.Count - 1)
            {
                percentage = 100m - equalPercentage * (splits.Count - 1);
            }

            bill.Splits.Add(new BillSplit
            {
                BillId = bill.Id,
                UserId = splitReq.UserId,
                Percentage = percentage,
                Amount = Math.Round(bill.Amount * percentage / 100m, 2),
                Status = splitReq.UserId == userId ? SplitStatus.Paid : SplitStatus.Unpaid
            });
        }

        // 8. Upload receipt
        memoryStream.Position = 0;
        var uniqueFileName = $"{bill.Id}/{Guid.CreateVersion7()}{Path.GetExtension(request.FileName)}";
        var receiptUrl = await fileStorageService.UploadAsync(
            ContainerName, uniqueFileName, memoryStream, request.ContentType, cancellationToken);

        bill.ReceiptUrl = receiptUrl;

        // 9. Persist
        dbContext.Bills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, userId),
            cancellationToken);

        // 10. Return full detail
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
            ReceiptUrl = bill.ReceiptUrl,
            Notes = bill.Notes,
            CreatedAt = bill.CreatedAt,
            CreatedBy = bill.CreatedBy,
            Splits = bill.Splits.Select(s => new BillSplitDto
            {
                Id = s.Id,
                UserId = s.UserId,
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
