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
    IMediator mediator,
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IReceiptAnalysisService receiptAnalysisService,
    IFileStorageService fileStorageService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<ProcessShoppingListReceiptCommand, ProcessReceiptResultDto>
{
    private const string ContainerName = "receipts";
    private const decimal OntarioHstRate = 0.13m;

    public async Task<ProcessReceiptResultDto> Handle(
        ProcessShoppingListReceiptCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var now = dateTimeProvider.UtcNow;

        // 1. Load current shopping list with items
        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        // 2. Load all other accessible shopping lists for cross-list matching
        var otherLists = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .Where(sl => !sl.IsDeleted && sl.Id != shoppingList.Id && !sl.IsCompleted)
            .Where(sl => sl.CreatedBy == userId
                || dbContext.EntityShares.Any(s =>
                    s.EntityType == EntityTypes.ShoppingList
                    && s.EntityId == sl.Id
                    && s.SharedWithUserId == userId
                    && !s.IsDeleted))
            .ToListAsync(cancellationToken);

        // 3. Buffer stream so it can be read twice (analysis + storage)
        using var memoryStream = new MemoryStream();
        await request.Content.CopyToAsync(memoryStream, cancellationToken);

        // 4. Extract existing item names for AI context (all lists)
        var existingItemNames = shoppingList.Items
            .Select(i => i.Name)
            .Concat(otherLists.SelectMany(l => l.Items.Select(i => i.Name)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        // 5. Analyze the receipt image with shopping list context
        memoryStream.Position = 0;
        var analysis = await receiptAnalysisService.AnalyzeAsync(
            memoryStream, request.ContentType, existingItemNames, cancellationToken);

        // 6. Build the bill entity
        var description = !string.IsNullOrWhiteSpace(analysis.StoreAddress)
            ? analysis.StoreAddress
            : null;

        var totalTax = analysis.Items
            .Where(i => i.IsTaxable)
            .Sum(i => Math.Round(i.Price * OntarioHstRate, 2));

        string? notes = null;
        var noteParts = new List<string>();
        if (analysis.Discount > 0)
            noteParts.Add($"Discount applied: {analysis.Discount:F2} {analysis.Currency}");
        if (totalTax > 0)
            noteParts.Add($"Tax (HST 13%): {totalTax:F2} {analysis.Currency}");
        if (noteParts.Count > 0)
            notes = string.Join(" | ", noteParts);

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

        bill.RelatedItems.Add(new BillRelatedItem
        {
            RelatedEntityId = shoppingList.Id,
            RelatedEntityType = EntityTypes.ShoppingList
        });

        if(shoppingList.DefaultBudgetId.HasValue)
        {
            // Find the active occurrence, or fall back to the most recent
            var activeOccurrence = await dbContext.BudgetOccurrences
                .Where(o => o.BudgetId == shoppingList.DefaultBudgetId.Value
                    && o.PeriodStart <= bill.BillDate && o.PeriodEnd >= bill.BillDate)
                .FirstOrDefaultAsync(cancellationToken);

            var usedFallback = false;
            if (activeOccurrence is null)
            {
                activeOccurrence = await dbContext.BudgetOccurrences
                    .Where(o => o.BudgetId == shoppingList.DefaultBudgetId.Value)
                    .OrderByDescending(o => o.PeriodStart)
                    .FirstOrDefaultAsync(cancellationToken);
                usedFallback = activeOccurrence is not null;
            }

            if (activeOccurrence is not null)
            {
                bill.RelatedItems.Add(new BillRelatedItem
                {
                    RelatedEntityId = shoppingList.DefaultBudgetId.Value,
                    RelatedEntityType = EntityTypes.Budget
                });

                bill.BudgetLink = new BillBudgetLink
                {
                    BudgetId = shoppingList.DefaultBudgetId.Value,
                    BillId = bill.Id,
                    BudgetOccurrenceId = activeOccurrence.Id
                };

                if (usedFallback)
                {
                    dbContext.Notifications.Add(new Notification
                    {
                        Title = "No Active Budget Period",
                        Description = $"Bill '{bill.Title}' was linked to the most recent budget period because no active period was found for the budget.",
                        Type = NotificationType.BudgetThresholdReached,
                        FromUserId = userId,
                        ToUserId = userId,
                        RelatedEntityId = shoppingList.DefaultBudgetId.Value,
                        RelatedEntityType = EntityTypes.Budget
                    });
                }
            }
        }

        // 7. Create bill items from analyzed receipt lines
        foreach (var lineItem in analysis.Items)
        {
            var unitPrice = lineItem.Quantity > 0
                ? Math.Round(lineItem.Price / lineItem.Quantity, 2)
                : lineItem.Price;

            var taxAmount = lineItem.IsTaxable
                ? Math.Round(lineItem.Price * OntarioHstRate, 2)
                : 0m;

            bill.Items.Add(new BillItem
            {
                BillId = bill.Id,
                Name = lineItem.Name,
                Quantity = lineItem.Quantity < 1 ? 1 : lineItem.Quantity,
                UnitPrice = unitPrice,
                Price = lineItem.Price,
                Discount = 0m,
                IsTaxable = lineItem.IsTaxable,
                TaxAmount = taxAmount,
                ShoppingListId = shoppingList.Id
            });
        }

        // 8. Distribute bill-level discount proportionally across items
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

        // 9. Create splits
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
                Status = SplitStatus.Paid 
            });
        }

        // 10. Upload receipt
        memoryStream.Position = 0;
        var uniqueFileName = $"{bill.Id}/{Guid.CreateVersion7()}{Path.GetExtension(request.FileName)}";
        var receiptUrl = await fileStorageService.UploadAsync(
            ContainerName, uniqueFileName, memoryStream, request.ContentType, cancellationToken);

        bill.ReceiptUrl = receiptUrl;

        // 11. Reconcile receipt items with current shopping list + cross-list detection
        var checkedItems = new List<ShoppingItemDto>();
        var addedItems = new List<ShoppingItemDto>();
        var crossListMatches = new List<CrossListMatchDto>();

        foreach (var (billItem, lineItem) in bill.Items.Zip(analysis.Items))
        {
            var genericName = !string.IsNullOrWhiteSpace(lineItem.GenericName)
                ? lineItem.GenericName
                : lineItem.Name;

            // Try to match in the current shopping list first
            var existingItem = shoppingList.Items.FirstOrDefault(
                i => i.Name.Equals(genericName, StringComparison.OrdinalIgnoreCase) && !i.IsChecked)
                ?? shoppingList.Items.FirstOrDefault(
                    i => i.Name.Equals(lineItem.Name, StringComparison.OrdinalIgnoreCase) && !i.IsChecked);

            if (existingItem is not null)
            {
                // Match found: toggle the existing item as checked
                existingItem.IsChecked = true;
                existingItem.CheckedAt = now;
                existingItem.CheckedByUserId = userId;
                billItem.ShoppingListId = shoppingList.Id;
                checkedItems.Add(MapToDto(existingItem));
            }
            else
            {
                // Check if an item with this name already exists (possibly already checked)
                var anyExistingInCurrent = shoppingList.Items.Any(
                    i => i.Name.Equals(genericName, StringComparison.OrdinalIgnoreCase)
                      || i.Name.Equals(lineItem.Name, StringComparison.OrdinalIgnoreCase));

                if (anyExistingInCurrent)
                    continue;

                // Check other lists for cross-list matches
                var matchingOtherLists = otherLists
                    .Select(list => new
                    {
                        List = list,
                        MatchingItem = list.Items.FirstOrDefault(
                            i => !i.IsChecked && (
                                i.Name.Equals(genericName, StringComparison.OrdinalIgnoreCase)
                                || i.Name.Equals(lineItem.Name, StringComparison.OrdinalIgnoreCase)))
                    })
                    .Where(x => x.MatchingItem is not null)
                    .ToList();

                if (matchingOtherLists.Count > 0)
                {
                    crossListMatches.Add(new CrossListMatchDto
                    {
                        ReceiptItemName = lineItem.Name,
                        GenericName = genericName,
                        Price = lineItem.Price,
                        Quantity = lineItem.Quantity,
                        IsTaxable = lineItem.IsTaxable,
                        MatchingLists = matchingOtherLists.Select(m => new CrossListTargetDto
                        {
                            ShoppingListId = m.List.Id,
                            ShoppingListTitle = m.List.Title,
                            ShoppingItemId = m.MatchingItem!.Id,
                            ShoppingItemName = m.MatchingItem.Name
                        }).ToList()
                    });
                }
                else
                {
                    // New item: add to shopping list with generic name
                    var nextSortOrder = shoppingList.Items.Count > 0
                        ? shoppingList.Items.Max(i => i.SortOrder) + 1
                        : 0;

                    var newItem = new ShoppingItem
                    {
                        ShoppingListId = shoppingList.Id,
                        Name = genericName,
                        Quantity = billItem.Quantity,
                        SortOrder = nextSortOrder
                    };

                    shoppingList.Items.Add(newItem);
                    dbContext.ShoppingItems.Add(newItem);
                    billItem.ShoppingListId = shoppingList.Id;
                    addedItems.Add(MapToDto(newItem));
                }
            }
        }

        // 12. Update completion state
        if (shoppingList.Items.Count > 0)
        {
            var allChecked = shoppingList.Items.All(i => i.IsChecked);
            shoppingList.IsCompleted = allChecked;
            shoppingList.CompletedAt = allChecked ? now : null;
        }

        // 13. Persist
        dbContext.Bills.Add(bill);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 14. Publish events
        await publisher.Publish(
            new BillCreatedEvent(bill.Id, bill.Title, bill.Amount, userId),
            cancellationToken);

        foreach (var item in checkedItems)
        {
            await publisher.Publish(
                new ShoppingItemCheckedEvent(shoppingList.Id, shoppingList.Title, item.Name, userId),
                cancellationToken);
        }

        // 15. Return result
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
                RelatedItems = bill.RelatedItems.Select(ri => new BillRelatedItemDto
                {
                    RelatedEntityId = ri.RelatedEntityId,
                    RelatedEntityType = ri.RelatedEntityType
                }).ToList(),
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
                    Discount = i.Discount,
                    IsTaxable = i.IsTaxable,
                    TaxAmount = i.TaxAmount,
                    ShoppingListId = i.ShoppingListId
                }).ToList()
            },
            CheckedItems = checkedItems,
            AddedItems = addedItems,
            CrossListMatches = crossListMatches
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
