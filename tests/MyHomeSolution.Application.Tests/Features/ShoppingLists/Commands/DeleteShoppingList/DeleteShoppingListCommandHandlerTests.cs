using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Features.ShoppingLists.Commands.DeleteShoppingList;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Features.ShoppingLists.Commands.DeleteShoppingList;

public sealed class DeleteShoppingListCommandHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly MediatR.IPublisher _publisher = Substitute.For<MediatR.IPublisher>();

    public DeleteShoppingListCommandHandlerTests()
    {
        _currentUserService.UserId.Returns("user-1");
    }

    [Fact]
    public async Task Handle_ShouldSoftDeleteShoppingList()
    {
        var list = await SeedShoppingList();

        using var context = _factory.CreateContext();
        var handler = new DeleteShoppingListCommandHandler(context, _currentUserService, _publisher);

        await handler.Handle(new DeleteShoppingListCommand(list.Id), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var deleted = await assertContext.ShoppingLists
            .IgnoreQueryFilters()
            .FirstAsync(sl => sl.Id == list.Id);
        deleted.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldPublishDeletedEvent()
    {
        var list = await SeedShoppingList();

        using var context = _factory.CreateContext();
        var handler = new DeleteShoppingListCommandHandler(context, _currentUserService, _publisher);

        await handler.Handle(new DeleteShoppingListCommand(list.Id), CancellationToken.None);

        await _publisher.Received(1).Publish(
            Arg.Is<ShoppingListDeletedEvent>(e => e.ShoppingListId == list.Id),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldThrow_WhenNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new DeleteShoppingListCommandHandler(context, _currentUserService, _publisher);

        var act = () => handler.Handle(
            new DeleteShoppingListCommand(Guid.CreateVersion7()), CancellationToken.None);
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
