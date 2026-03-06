using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.ToggleShoppingItem;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.ToggleShoppingItem;

public sealed class ToggleShoppingItemCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    private static readonly DateTimeOffset FixedNow = new(2025, 7, 1, 12, 0, 0, TimeSpan.Zero);

    public ToggleShoppingItemCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
        _dateTimeProvider.UtcNow.Returns(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldCheckItem_WhenUnchecked()
    {
        var (list, item) = await SeedShoppingListWithItem(isChecked: false);

        using var context = _factory.CreateContext();
        var handler = new ToggleShoppingItemCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new ToggleShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = item.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.ShoppingItems.FirstAsync(i => i.Id == item.Id);
        updated.IsChecked.Should().BeTrue();
        updated.CheckedAt.Should().Be(FixedNow);
        updated.CheckedByUserId.Should().Be("user-1");
    }

    [Fact]
    public async Task Handle_ShouldUncheckItem_WhenChecked()
    {
        var (list, item) = await SeedShoppingListWithItem(isChecked: true);

        using var context = _factory.CreateContext();
        var handler = new ToggleShoppingItemCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new ToggleShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = item.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.ShoppingItems.FirstAsync(i => i.Id == item.Id);
        updated.IsChecked.Should().BeFalse();
        updated.CheckedAt.Should().BeNull();
        updated.CheckedByUserId.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldMarkListAsCompleted_WhenAllItemsChecked()
    {
        var (list, item) = await SeedShoppingListWithItem(isChecked: false);

        using var context = _factory.CreateContext();
        var handler = new ToggleShoppingItemCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new ToggleShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = item.Id
        }, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updatedList = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updatedList.IsCompleted.Should().BeTrue();
        updatedList.CompletedAt.Should().Be(FixedNow);
    }

    [Fact]
    public async Task Handle_ShouldPublishEvent_WhenItemChecked()
    {
        var (list, item) = await SeedShoppingListWithItem(isChecked: false);

        using var context = _factory.CreateContext();
        var handler = new ToggleShoppingItemCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new ToggleShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = item.Id
        }, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ShoppingItemCheckedEvent>(e =>
                e.ShoppingListId == list.Id && e.ItemName == "Eggs"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotPublishEvent_WhenItemUnchecked()
    {
        var (list, item) = await SeedShoppingListWithItem(isChecked: true);

        using var context = _factory.CreateContext();
        var handler = new ToggleShoppingItemCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        await handler.Handle(new ToggleShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = item.Id
        }, CancellationToken.None);

        await _publisher.DidNotReceive().Publish(
            Arg.Any<ShoppingItemCheckedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenItemNotFound()
    {
        var (list, _) = await SeedShoppingListWithItem(isChecked: false);

        using var context = _factory.CreateContext();
        var handler = new ToggleShoppingItemCommandHandler(
            context, _currentUserService, _dateTimeProvider, _publisher);

        var act = () => handler.Handle(new ToggleShoppingItemCommand
        {
            ShoppingListId = list.Id,
            ItemId = Guid.CreateVersion7()
        }, CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private async Task<(ShoppingList list, ShoppingItem item)> SeedShoppingListWithItem(bool isChecked)
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
            SortOrder = 0,
            IsChecked = isChecked,
            CheckedAt = isChecked ? FixedNow.AddHours(-1) : null,
            CheckedByUserId = isChecked ? "user-2" : null
        };
        list.Items.Add(item);
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return (list, item);
    }

    public void Dispose() => _factory.Dispose();
}
