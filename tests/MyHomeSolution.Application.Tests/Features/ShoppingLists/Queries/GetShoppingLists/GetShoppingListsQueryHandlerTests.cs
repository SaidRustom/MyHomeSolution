using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingLists;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Queries.GetShoppingLists;

public sealed class GetShoppingListsQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetShoppingListsQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnOwnedLists()
    {
        await SeedLists();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListsQuery(), CancellationToken.None);

        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldFilterByCategory()
    {
        await SeedLists();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListsQuery
        {
            Category = ShoppingListCategory.Groceries
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Category.Should().Be(ShoppingListCategory.Groceries);
    }

    [Fact]
    public async Task Handle_ShouldFilterByIsCompleted()
    {
        await SeedLists();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListsQuery
        {
            IsCompleted = true
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldFilterBySearchTerm()
    {
        await SeedLists();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListsQuery
        {
            SearchTerm = "Weekly"
        }, CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.First().Title.Should().Contain("Weekly");
    }

    [Fact]
    public async Task Handle_ShouldIncludeItemCounts()
    {
        await SeedListWithItems();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListsQuery(), CancellationToken.None);

        var list = result.Items.First();
        list.TotalItems.Should().Be(2);
        list.CheckedItems.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(new GetShoppingListsQuery(), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldNotReturnDeletedLists()
    {
        using var seedContext = _factory.CreateContext();
        seedContext.ShoppingLists.Add(new ShoppingList
        {
            Title = "Deleted",
            Category = ShoppingListCategory.General,
            CreatedBy = "user-1",
            IsDeleted = true
        });
        await seedContext.SaveChangesAsync();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListsQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListsQuery(), CancellationToken.None);
        result.Items.Should().BeEmpty();
    }

    private async Task SeedLists()
    {
        using var context = _factory.CreateContext();
        context.ShoppingLists.Add(new ShoppingList
        {
            Title = "Weekly Groceries",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        });
        context.ShoppingLists.Add(new ShoppingList
        {
            Title = "Completed Supplies",
            Category = ShoppingListCategory.Household,
            CreatedBy = "user-1",
            IsCompleted = true,
            CompletedAt = DateTimeOffset.UtcNow
        });
        context.ShoppingLists.Add(new ShoppingList
        {
            Title = "Someone Else's List",
            Category = ShoppingListCategory.General,
            CreatedBy = "user-2"
        });
        await context.SaveChangesAsync();
    }

    private async Task SeedListWithItems()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "With Items",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        list.Items.Add(new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Eggs",
            Quantity = 12,
            SortOrder = 0,
            IsChecked = true,
            CheckedAt = DateTimeOffset.UtcNow,
            CheckedByUserId = "user-1"
        });
        list.Items.Add(new ShoppingItem
        {
            ShoppingListId = list.Id,
            Name = "Milk",
            Quantity = 1,
            SortOrder = 1
        });
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
    }

    public void Dispose() => _factory.Dispose();
}
