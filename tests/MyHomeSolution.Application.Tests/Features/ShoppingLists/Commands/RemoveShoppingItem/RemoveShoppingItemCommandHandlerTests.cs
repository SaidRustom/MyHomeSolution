using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.RemoveShoppingItem;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.RemoveShoppingItem;

public sealed class RemoveShoppingItemCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private static readonly DateTimeOffset Now = new(2025, 7, 10, 12, 0, 0, TimeSpan.Zero);

    public RemoveShoppingItemCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(Now);
    }

    [Fact]
    public async Task Handle_ShouldRemoveItemFromList()
    {
        var (list, item) = await SeedShoppingListWithItem();

        using var context = _factory.CreateContext();
        var handler = new RemoveShoppingItemCommandHandler(context, _currentUserService, _dateTimeProvider);

        await handler.Handle(new RemoveShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = item.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.ShoppingItems
            .CountAsync(i => i.ShoppingListId == list.Id);
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenItemNotFound()
    {
        var (list, _) = await SeedShoppingListWithItem();

        using var context = _factory.CreateContext();
        var handler = new RemoveShoppingItemCommandHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(new RemoveShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenListNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new RemoveShoppingItemCommandHandler(context, _currentUserService, _dateTimeProvider);

        var act = () => handler.Handle(new RemoveShoppingItemCommand
        {
            ShoppingListId = Guid.CreateVersion7(),
            ItemId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldCompleteList_WhenLastUncheckedItemRemoved()
    {
        using var seedContext = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Test",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        var checkedItem = new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Milk",
            Quantity = 1,
            SortOrder = 0,
            IsChecked = true,
            CheckedAt = Now.AddHours(-1),
            CheckedByUserId = "user-1"
        };
        var uncheckedItem = new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Bread",
            Quantity = 1,
            SortOrder = 1
        };
        list.Items.Add(checkedItem);
        list.Items.Add(uncheckedItem);
        seedContext.ShoppingLists.Add(list);
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new RemoveShoppingItemCommandHandler(context, _currentUserService, _dateTimeProvider);

        await handler.Handle(new RemoveShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = uncheckedItem.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updatedList = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updatedList.IsCompleted.Should().BeTrue();
        updatedList.CompletedAt.Should().Be(Now);
    }

    private async Task<(ShoppingList list, ShoppingItem item)> SeedShoppingListWithItem()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Test List",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        var item = new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Eggs",
            Quantity = 12,
            SortOrder = 0
        };
        list.Items.Add(item);
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return (list, item);
    }

    public void Dispose() => _factory.Dispose();
}
