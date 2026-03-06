using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.AddShoppingItemFromBillItem;

public sealed class AddShoppingItemFromBillItemCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public AddShoppingItemFromBillItemCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldAddItemFromBillItem()
    {
        var (list, billItem) = await SeedData();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        var result = await handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = list.Id,
            BillItemId = billItem.Id
        }, CancellationToken.None);

        result.Name.Should().Be("Organic Milk");
        result.Quantity.Should().Be(2);
        result.Notes.Should().Contain("unit price");
        result.IsChecked.Should().BeFalse();

        using var assertContext = _factory.CreateContext();
        var items = await assertContext.ShoppingItems
            .Where(i => i.ShoppingListId == list.Id)
            .ToListAsync();
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldUseQuantityOverride_WhenProvided()
    {
        var (list, billItem) = await SeedData();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        var result = await handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = list.Id,
            BillItemId = billItem.Id,
            QuantityOverride = 5
        }, CancellationToken.None);

        result.Quantity.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldUseUnitOverride_WhenProvided()
    {
        var (list, billItem) = await SeedData();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        var result = await handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = list.Id,
            BillItemId = billItem.Id,
            UnitOverride = "gallons"
        }, CancellationToken.None);

        result.Unit.Should().Be("gallons");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenDuplicateItemExists()
    {
        var (list, billItem) = await SeedDataWithExistingItem();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = list.Id,
            BillItemId = billItem.Id
        }, CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenBillItemNotFound()
    {
        var (list, _) = await SeedData();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = list.Id,
            BillItemId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenShoppingListNotFound()
    {
        var (_, billItem) = await SeedData();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = Guid.CreateVersion7(),
            BillItemId = billItem.Id
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldResetCompletedState_WhenListWasCompleted()
    {
        using var seedContext = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Receipt",
            Amount = 10m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1"
        };
        var billItem = new BillItem
        {
            BillId = bill.Id,
            Name = "Water",
            Quantity = 1,
            UnitPrice = 2m,
            Price = 2m
        };
        bill.Items.Add(billItem);
        seedContext.Bills.Add(bill);

        var list = new ShoppingList
        {
            Title = "Completed List",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1",
            IsCompleted = true,
            CompletedAt = DateTimeOffset.UtcNow.AddHours(-1)
        };
        seedContext.ShoppingLists.Add(list);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemFromBillItemCommandHandler(context, _currentUserService);

        await handler.Handle(new AddShoppingItemFromBillItemCommand
        {
            ShoppingListId = list.Id,
            BillItemId = billItem.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updatedList = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updatedList.IsCompleted.Should().BeFalse();
        updatedList.CompletedAt.Should().BeNull();
    }

    private async Task<(ShoppingList list, BillItem billItem)> SeedData()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Grocery Receipt",
            Amount = 25m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1"
        };
        var billItem = new BillItem
        {
            BillId = bill.Id,
            Name = "Organic Milk",
            Quantity = 2,
            UnitPrice = 4.99m,
            Price = 9.98m
        };
        bill.Items.Add(billItem);
        context.Bills.Add(bill);

        var list = new ShoppingList
        {
            Title = "Weekly Groceries",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return (list, billItem);
    }

    private async Task<(ShoppingList list, BillItem billItem)> SeedDataWithExistingItem()
    {
        using var context = _factory.CreateContext();
        var bill = new Bill
        {
            Title = "Grocery Receipt",
            Amount = 10m,
            Currency = "USD",
            Category = BillCategory.Groceries,
            BillDate = DateTimeOffset.UtcNow,
            PaidByUserId = "user-1"
        };
        var billItem = new BillItem
        {
            BillId = bill.Id,
            Name = "Organic Milk",
            Quantity = 2,
            UnitPrice = 4.99m,
            Price = 9.98m
        };
        bill.Items.Add(billItem);
        context.Bills.Add(bill);

        var list = new ShoppingList
        {
            Title = "Weekly Groceries",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        list.Items.Add(new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Organic Milk",
            Quantity = 1,
            SortOrder = 0
        });
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return (list, billItem);
    }

    public void Dispose() => _factory.Dispose();
}
