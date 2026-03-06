using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.AddShoppingItem;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.AddShoppingItem;

public sealed class AddShoppingItemCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public AddShoppingItemCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldAddItemToShoppingList()
    {
        var list = await SeedShoppingList();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemCommandHandler(context, _currentUserService);

        var command = new AddShoppingItemCommand
        {
            ShoppingListId = list.Id,
            Name = "Milk",
            Quantity = 2,
            Unit = "liters",
            Notes = "Whole milk"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Name.Should().Be("Milk");
        result.Quantity.Should().Be(2);
        result.Unit.Should().Be("liters");

        using var assertContext = _factory.CreateContext();
        var items = await assertContext.ShoppingItems
            .Where(i => i.ShoppingListId == list.Id)
            .ToListAsync();
        items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldAssignIncrementingSortOrder()
    {
        var list = await SeedShoppingListWithItem();

        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemCommandHandler(context, _currentUserService);

        var result = await handler.Handle(new AddShoppingItemCommand
        {
            ShoppingListId = list.Id,
            Name = "Bread"
        }, CancellationToken.None);

        result.SortOrder.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenShoppingListNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new AddShoppingItemCommandHandler(context, _currentUserService);

        var act = () => handler.Handle(new AddShoppingItemCommand
        {
            ShoppingListId = Guid.CreateVersion7(),
            Name = "Test"
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<ShoppingList> SeedShoppingList()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Test List",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return list;
    }

    private async Task<ShoppingList> SeedShoppingListWithItem()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Test List",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        list.Items.Add(new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Eggs",
            Quantity = 1,
            SortOrder = 0
        });
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return list;
    }

    public void Dispose() => _factory.Dispose();
}
