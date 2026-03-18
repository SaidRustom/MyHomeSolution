using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.ResolveCrossListMatch;

public sealed class ResolveCrossListMatchCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<ResolveCrossListMatchCommand>
{
    private const decimal OntarioHstRate = 0.13m;

    public async Task Handle(ResolveCrossListMatchCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var now = dateTimeProvider.UtcNow;

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.TargetShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.TargetShoppingListId);

        // Verify the bill exists
        var bill = await dbContext.Bills
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == request.BillId && !b.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(Bill), request.BillId);

        if (request.ToggleExisting && request.ShoppingItemId.HasValue)
        {
            // Toggle an existing item as checked
            var item = shoppingList.Items.FirstOrDefault(i => i.Id == request.ShoppingItemId.Value)
                ?? throw new NotFoundException(nameof(ShoppingItem), request.ShoppingItemId.Value);

            if (!item.IsChecked)
            {
                item.IsChecked = true;
                item.CheckedAt = now;
                item.CheckedByUserId = userId;
            }

            // Update the corresponding bill item's ShoppingListId
            var billItem = bill.Items.FirstOrDefault(
                i => i.Name.Equals(request.ReceiptItemName, StringComparison.OrdinalIgnoreCase));
            if (billItem is not null)
            {
                billItem.ShoppingListId = shoppingList.Id;
            }

            await publisher.Publish(
                new ShoppingItemCheckedEvent(shoppingList.Id, shoppingList.Title, item.Name, userId),
                cancellationToken);
        }
        else
        {
            // Add as a new item to the target list
            var nextSortOrder = shoppingList.Items.Count > 0
                ? shoppingList.Items.Max(i => i.SortOrder) + 1
                : 0;

            var taxAmount = request.IsTaxable
                ? Math.Round(request.Price * OntarioHstRate, 2)
                : 0m;

            var newItem = new ShoppingItem
            {
                ShoppingListId = shoppingList.Id,
                Name = request.GenericName,
                Quantity = 1,
                Notes = $"Added from receipt ({request.ReceiptItemName}, price: {request.Price:F2}{(taxAmount > 0 ? $" + tax: {taxAmount:F2}" : "")})",
                IsChecked = true,
                CheckedAt = now,
                CheckedByUserId = userId,
                SortOrder = nextSortOrder
            };

            shoppingList.Items.Add(newItem);
            dbContext.ShoppingItems.Add(newItem);

            // Update bill item association
            var billItem = bill.Items.FirstOrDefault(
                i => i.Name.Equals(request.ReceiptItemName, StringComparison.OrdinalIgnoreCase));
            if (billItem is not null)
            {
                billItem.ShoppingListId = shoppingList.Id;
            }
        }

        // Update completion state
        if (shoppingList.Items.Count > 0)
        {
            var allChecked = shoppingList.Items.All(i => i.IsChecked);
            shoppingList.IsCompleted = allChecked;
            shoppingList.CompletedAt = allChecked ? now : null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
