using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Constants;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.Bills.Common;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ProcessShoppingListReceipt;

public sealed class ProcessShoppingListReceiptCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IReceiptAnalysisService receiptAnalysisService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<ProcessShoppingListReceiptCommand, ProcessReceiptResultDto>
{
    private const string ContainerName = "receipts";

    public async Task<ProcessReceiptResultDto> Handle(
        ProcessShoppingListReceiptCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var now = dateTimeProvider.UtcNow;

        // 1. Load shopping list with items
        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        // 2. Buffer stream so it can be read twice (analysis + storage)
        using var memoryStream = new MemoryStream();
        await request.Content.CopyToAsync(memoryStream, cancellationToken);

        // 3. Extract existing item names for AI context
        var existingItemNames = shoppingList.Items
            .Select(i => i.Name)
            .ToList();

        // 4. Analyze the receipt image with shopping list context
        memoryStream.Position = 0;
        var analysis = await receiptAnalysisService.AnalyzeAsync(
            memoryStream, request.ContentType, existingItemNames, cancellationToken);

        // 5. Build the bill entity
        var description = !string.IsNullOrWhiteSpace(analysis.StoreAddress)
            ? analysis.StoreAddress
            : null;

        string? notes = analysis.Discount > 0
            ? $"Discount applied: {analysis.Discount:F2} {analysis.Currency}"
            : null;

        var bill = new Bill
        {
            Title = analysis.StoreName,
            Description = description,
            Amount = analysis.Total,
            Currency = analysis.Currency,
            Category = MapCategory(shoppingList.Category),
            BillDate = analysis.TransactionDate != default
                ? analysis.TransactionDate
                : now,
            PaidByUserId = userId,
            Notes = notes,
            RelatedEntityId = shoppingList.Id,
            RelatedEntityType = EntityTypes.ShoppingList
        };

        // 6. Create bill items from analyzed receipt lines
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

        // 7. Distribute bill-level discount proportionally across items
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

        // 8. Create splits
        var splits = request.Splits ?? [new ReceiptSplitRequest { UserId = userId }];
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

        // 9. Upload receipt
        memoryStream.Position = 0;
        var uniqueFileName = $"{bill.Id}/{Guid.CreateVersion7()}{Path.GetExtension(request.FileName)}";
        var receiptUrl = await fileStorageService.UploadAsync(
            ContainerName, uniqueFileName, memoryStream, request.ContentType, cancellationToken);

        bill.ReceiptUrl = receiptUrl;

        // 10. Reconcile receipt items with shopping list
        var checkedItems = new List<ShoppingItemDto>();
        var addedItems = new List<ShoppingItemDto>();

        foreach (var billItem in bill.Items)
        {
            var existingItem = shoppingList.Items.FirstOrDefault(
                i => i.Name.Equals(billItem.Name, StringComparison.OrdinalIgnoreCase) && !i.IsChecked);

            if (existingItem is not null)
            {
                // Match found: toggle the existing item as checked
                existingItem.IsChecked = true;
                existingItem.CheckedAt = now;
                existingItem.CheckedByUserId = userId;
                checkedItems.Add(MapToDto(existingItem));
            }
            else
            {
                // Check if an item with this name already exists (possibly already checked)
                var anyExisting = shoppingList.Items.Any(
                    i => i.Name.Equals(billItem.Name, StringComparison.OrdinalIgnoreCase));

                if (!anyExisting)
                {
                    // New item: add to shopping list (AddShoppingItemFromBillItem behavior)
                    var nextSortOrder = shoppingList.Items.Count > 0
                        ? shoppingList.Items.Max(i => i.SortOrder) + 1
                        : 0;

                    var newItem = new ShoppingItem
                    {
                        ShoppingListId = shoppingList.Id,
                        Name = billItem.Name,
                        Quantity = billItem.Quantity,
                        Notes = $"Added from receipt (unit price: {billItem.UnitPrice:F2} {bill.Currency})",
                        SortOrder = nextSortOrder
                    };

                    shoppingList.Items.Add(newItem);
                    dbContext.ShoppingItems.Add(newItem);
                    addedItems.Add(MapToDto(newItem));
                }
            }
        }

        // 11. Update completion state
        if (shoppingList.Items.Count > 0)
        {
            var allChecked = shoppingList.Items.All(i => i.IsChecked);
            shoppingList.IsCompleted = allChecked;
            shoppingList.CompletedAt = allChecked ? now : null;
        }

        // 12. Persist
        dbContext.Bills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 13. Publish events
        await publisher.Publish(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, userId),
            cancellationToken);

        foreach (var item in checkedItems)
        {
            await publisher.Publish(
                new ShoppingItemCheckedEvent(shoppingList.Id, shoppingList.Title, item.Name, userId),
                cancellationToken);
        }

        // 14. Return result
        return new ProcessReceiptResultDto
        {
            BillId = bill.Id,
            Bill = new BillDetailDto
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
                RelatedEntityId = bill.RelatedEntityId,
                RelatedEntityType = bill.RelatedEntityType,
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
            },
            CheckedItems = checkedItems,
            AddedItems = addedItems
        };
    }

    private static ShoppingItemDto MapToDto(ShoppingItem item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Quantity = item.Quantity,
        Unit = item.Unit,
        Notes = item.Notes,
        IsChecked = item.IsChecked,
        CheckedAt = item.CheckedAt,
        CheckedByUserId = item.CheckedByUserId,
        SortOrder = item.SortOrder
    };

    private static BillCategory MapCategory(ShoppingListCategory category) => category switch
    {
        ShoppingListCategory.Groceries => BillCategory.Groceries,
        ShoppingListCategory.Household => BillCategory.Supplies,
        _ => BillCategory.General
    };
}
