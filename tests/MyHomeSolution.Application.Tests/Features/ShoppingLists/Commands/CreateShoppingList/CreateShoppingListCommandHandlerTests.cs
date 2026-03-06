using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.CreateShoppingList;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.CreateShoppingList;

public sealed class CreateShoppingListCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public CreateShoppingListCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldCreateShoppingList()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateShoppingListCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateShoppingListCommand
        {
            Title = "Weekly Groceries",
            Description = "Groceries for the week",
            Category = ShoppingListCategory.Groceries,
            DueDate = new DateOnly(2025, 7, 15)
        };

        var id = await handler.Handle(command, CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var list = await assertContext.ShoppingLists.FirstOrDefaultAsync(sl => sl.Id == id);
        list.Should().NotBeNull();
        list!.Title.Should().Be("Weekly Groceries");
        list.Description.Should().Be("Groceries for the week");
        list.Category.Should().Be(ShoppingListCategory.Groceries);
        list.DueDate.Should().Be(new DateOnly(2025, 7, 15));
        list.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldPublishCreatedEvent()
    {
        using var context = _factory.CreateContext();
        var handler = new CreateShoppingListCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateShoppingListCommand
        {
            Title = "Household Supplies",
            Category = ShoppingListCategory.Household
        };

        await handler.Handle(command, CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ShoppingListCreatedEvent>(e =>
                e.Title == "Household Supplies" && e.CreatedByUserId == "user-1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenUserNotAuthenticated()
    {
        _currentUserService.UserId.Returns((string?)null);

        using var context = _factory.CreateContext();
        var handler = new CreateShoppingListCommandHandler(context, _currentUserService, _publisher);

        var command = new CreateShoppingListCommand
        {
            Title = "Test",
            Category = ShoppingListCategory.General
        };

        var act = () => handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    public void Dispose() => _factory.Dispose();
}
