using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class EntitySharedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public EntitySharedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldPersistNotification_ForSharedWithUser()
    {
        using var context = _factory.CreateContext();
        var handler = new EntitySharedNotificationHandler(context, _realtimeService, _dateTimeProvider);
        var entityId = Guid.CreateVersion7();

        await handler.Handle(
            new EntitySharedEvent(Guid.CreateVersion7(), "HouseholdTask", entityId, "shared-user", "owner-user"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "shared-user");
        notification.Should().NotBeNull();
        notification!.Title.Should().Contain("HouseholdTask");
        notification.Type.Should().Be(NotificationType.ShareReceived);
        notification.FromUserId.Should().Be("owner-user");
        notification.RelatedEntityId.Should().Be(entityId);
        notification.RelatedEntityType.Should().Be("HouseholdTask");
    }

    [Fact]
    public async Task Handle_ShouldPushRealtimeNotification()
    {
        using var context = _factory.CreateContext();
        var handler = new EntitySharedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new EntitySharedEvent(Guid.CreateVersion7(), "HouseholdTask", Guid.CreateVersion7(), "target", "sharer"),
            CancellationToken.None);

        await _realtimeService.Received(1).SendUserNotificationAsync(
            "target",
            Arg.Is<UserPushNotification>(n => n.Title!.Contains("HouseholdTask")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenSharingWithSelf()
    {
        using var context = _factory.CreateContext();
        var handler = new EntitySharedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(
            new EntitySharedEvent(Guid.CreateVersion7(), "HouseholdTask", Guid.CreateVersion7(), "user-1", "user-1"),
            CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    public void Dispose() => _factory.Dispose();
}
