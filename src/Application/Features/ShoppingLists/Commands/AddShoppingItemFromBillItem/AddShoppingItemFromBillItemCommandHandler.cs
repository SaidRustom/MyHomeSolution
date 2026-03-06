using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Common;
using MyHomeSolution.Domain.Entities;

namespace MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;

public sealed class AddShoppingItemFromBillItemCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService)
    : IRequestHandler<AddShoppingItemFromBillItemCommand, ShoppingItemDto>
{
    public async Task<ShoppingItemDto> Handle(
        AddShoppingItemFromBillItemCommand request, CancellationToken cancellationToken)
    {
        _ = currentUserService.UserId
            ?? throw new ForbiddenAccessException();

        var billItem = await dbContext.BillItems
            .AsNoTracking()
            .FirstOrDefaultAsync(bi => bi.Id == request.BillItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(BillItem), request.BillItemId);

        var shoppingList = await dbContext.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == request.ShoppingListId && !sl.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(ShoppingList), request.ShoppingListId);

        var duplicate = shoppingList.Items
            .Any(i => i.Name.Equals(billItem.Name, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new ConflictException(
                $"An item named '{billItem.Name}' already exists on this shopping list.");
        }

        var nextSortOrder = shoppingList.Items.Count > 0
            ? shoppingList.Items.Max(i => i.SortOrder) + 1
            : 0;

        var item = new ShoppingItem
        {
            ShoppingListId = shoppingList.Id,
            Name = billItem.Name,
            Quantity = request.QuantityOverride ?? billItem.Quantity,
            Unit = request.UnitOverride,
            Notes = $"Added from bill item (unit price: {billItem.UnitPrice:F2})",
            SortOrder = nextSortOrder
        };

        dbContext.ShoppingItems.Add(item);

        if (shoppingList.IsCompleted)
        {
            shoppingList.IsCompleted = false;
            shoppingList.CompletedAt = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShoppingItemDto
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
    }
}
