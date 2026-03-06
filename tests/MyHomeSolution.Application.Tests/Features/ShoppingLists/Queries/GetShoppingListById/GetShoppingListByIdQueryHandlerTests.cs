using FluentAssertions;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Queries.GetShoppingListById;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Queries.GetShoppingListById;

public sealed class GetShoppingListByIdQueryHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    public GetShoppingListByIdQueryHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldReturnShoppingListWithItems()
    {
        var list = await SeedListWithItems();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListByIdQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListByIdQuery(list.Id), CancellationToken.None);

        result.Id.Should().Be(list.Id);
        result.Title.Should().Be("Test List");
        result.Items.Should().HaveCount(2);
        result.Items.Should().BeInAscendingOrder(i => i.SortOrder);
    }

    [Fact]
    public async Task Handle_ShouldReturnAllItemFields()
    {
        var list = await SeedListWithItems();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListByIdQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListByIdQuery(list.Id), CancellationToken.None);

        var checkedItem = result.Items.First(i => i.IsChecked);
        checkedItem.Name.Should().Be("Eggs");
        checkedItem.Quantity.Should().Be(12);
        checkedItem.CheckedByUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new GetShoppingListByIdQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(
            new GetShoppingListByIdQuery(Guid.CreateVersion7()), CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserHasNoAccess()
    {
        _currentUserService.UserId.Returns("user-other");

        var list = await SeedListWithItems();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListByIdQueryHandler(context, _currentUserService);

        var act = () => handler.Handle(new GetShoppingListByIdQuery(list.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_ShouldAllowAccess_WhenListIsShared()
    {
        _currentUserService.UserId.Returns("user-shared");

        var list = await SeedSharedList();

        using var context = _factory.CreateContext();
        var handler = new GetShoppingListByIdQueryHandler(context, _currentUserService);

        var result = await handler.Handle(new GetShoppingListByIdQuery(list.Id), CancellationToken.None);
        result.Id.Should().Be(list.Id);
    }

    private async Task<ShoppingList> SeedListWithItems()
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
        return list;
    }

    private async Task<ShoppingList> SeedSharedList()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Shared List",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        context.ShoppingLists.Add(list);

        context.EntityShares.Add(new EntityShare
        {
            EntityId = list.Id,
            EntityType = "ShoppingList",
            SharedWithUserId = "user-shared",
            Permission = SharePermission.View
        });

        await context.SaveChangesAsync();
        return list;
    }

    public void Dispose() => _factory.Dispose();
}
