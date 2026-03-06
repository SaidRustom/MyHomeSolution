using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class ShoppingItemCheckedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public ShoppingItemCheckedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 7, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldNotifyOwnerAndSharedUsers()
    {
        var list = await SeedSharedList();

        using var context = _factory.CreateContext();
        var handler = new ShoppingItemCheckedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ShoppingItemCheckedEvent(list.Id, list.Title, "Eggs", "user-shared"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notifications = await assertContext.Notifications.ToListAsync();
        notifications.Should().HaveCount(1);
        notifications.First().ToUserId.Should().Be("user-1");
        notifications.First().Type.Should().Be(NotificationType.ShoppingItemChecked);
    }

    [Fact]
    public async Task Handle_ShouldDoNothing_WhenListNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new ShoppingItemCheckedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ShoppingItemCheckedEvent(Guid.CreateVersion7(), "Missing", "Eggs", "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotNotifyCheckerThemselves()
    {
        var list = await SeedOwnedList();

        using var context = _factory.CreateContext();
        var handler = new ShoppingItemCheckedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ShoppingItemCheckedEvent(list.Id, list.Title, "Eggs", "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotification()
    {
        var list = await SeedSharedList();

        using var context = _factory.CreateContext();
        var handler = new ShoppingItemCheckedNotificationHandler(
            context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new ShoppingItemCheckedEvent(list.Id, list.Title, "Eggs", "user-shared"),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "user-1",
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("Eggs")),
            Arg.Any<CancellationToken>());
    }

    private async Task<ShoppingList> SeedSharedList()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "Groceries",
            Category = ShoppingListCategory.Groceries,
            CreatedBy = "user-1"
        };
        context.ShoppingLists.Add(list);

        context.EntityShares.Add(new EntityShare
        {
            EntityId = list.Id,
            EntityType = "ShoppingList",
            SharedWithUserId = "user-shared",
            Permission = SharePermission.Edit
        });

        await context.SaveChangesAsync();
        return list;
    }

    private async Task<ShoppingList> SeedOwnedList()
    {
        using var context = _factory.CreateContext();
        var list = new ShoppingList
        {
            Title = "My List",
            Category = ShoppingListCategory.General,
            CreatedBy = "user-1"
        };
        context.ShoppingLists.Add(list);
        await context.SaveChangesAsync();
        return list;
    }

    public void Dispose() => _factory.Dispose();
}
