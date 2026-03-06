using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.UpdateShoppingList;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.UpdateShoppingList;

public sealed class UpdateShoppingListCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public UpdateShoppingListCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldUpdateShoppingList()
    {
        var list = await SeedShoppingList();

        using var context = _factory.CreateContext();
        var handler = new UpdateShoppingListCommandHandler(context, _currentUserService, _publisher);

        var command = new UpdateShoppingListCommand
        {
            Id = list.Id,
            Title = "Updated Title",
            Description = "Updated Description",
            Category = ShoppingListCategory.Household,
            DueDate = new DateOnly(2025, 8, 1)
        };

        await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var updated = await assertContext.ShoppingLists.FirstAsync(sl => sl.Id == list.Id);
        updated.Title.Should().Be("Updated Title");
        updated.Description.Should().Be("Updated Description");
        updated.Category.Should().Be(ShoppingListCategory.Household);
        updated.DueDate.Should().Be(new DateOnly(2025, 8, 1));
    }

    [Fact]
    public async Task Handle_ShouldPublishUpdatedEvent()
    {
        var list = await SeedShoppingList();

        using var context = _factory.CreateContext();
        var handler = new UpdateShoppingListCommandHandler(context, _currentUserService, _publisher);

        await handler.Handle(new UpdateShoppingListCommand
        {
            Id = list.Id,
            Title = "Changed",
            Category = ShoppingListCategory.General
        }, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ShoppingListUpdatedEvent>(e => e.ShoppingListId == list.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new UpdateShoppingListCommandHandler(context, _currentUserService, _publisher);

        var act = () => handler.Handle(new UpdateShoppingListCommand
        {
            Id = Guid.CreateVersion7(),
            Title = "Does not exist",
            Category = ShoppingListCategory.General
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

    public void Dispose() => _factory.Dispose();
}
