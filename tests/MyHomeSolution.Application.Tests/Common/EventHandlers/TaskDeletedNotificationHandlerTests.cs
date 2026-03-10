using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Tests.Testing;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class TaskDeletedNotificationHandlerTests : IDisposable
{
    private readonly TestDbContextFactory _factory = new();
    private readonly IRealtimeNotificationService _realtimeService = Substitute.For<IRealtimeNotificationService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    public TaskDeletedNotificationHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldPersistNotification_WhenAssigneeDiffersFromDeleter()
    {
        var task = await SeedDeletedTask("deleter-id", "assignee-id");

        using var context = _factory.CreateContext();
        var handler = new TaskDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskDeletedEvent(task.Id, task.Title, 0, 0, []), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var notification = await assertContext.Notifications.FirstOrDefaultAsync(n => n.ToUserId == "assignee-id");
        notification.Should().NotBeNull();
        notification!.Title.Should().Contain("Deleted task");
        notification.Type.Should().Be(NotificationType.TaskDeleted);
        notification.FromUserId.Should().Be("deleter-id");
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenAssigneeIsDeleter()
    {
        var task = await SeedDeletedTask("user-1", "user-1");

        using var context = _factory.CreateContext();
        var handler = new TaskDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskDeletedEvent(task.Id, task.Title, 0, 0, []), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotCreateNotification_WhenNoAssignee()
    {
        var task = await SeedDeletedTask("deleter-id", assignedTo: null);

        using var context = _factory.CreateContext();
        var handler = new TaskDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        await handler.Handle(new TaskDeletedEvent(task.Id, task.Title, 0, 0, []), CancellationToken.None);

        using var assertContext = _factory.CreateContext();
        var count = await assertContext.Notifications.CountAsync();
        count.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldNotThrow_WhenTaskNotFound()
    {
        using var context = _factory.CreateContext();
        var handler = new TaskDeletedNotificationHandler(context, _realtimeService, _dateTimeProvider);

        var act = () => handler.Handle(
            new TaskDeletedEvent(Guid.CreateVersion7(), "Unknown", 0, 0, []), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private async Task<HouseholdTask> SeedDeletedTask(string deletedBy, string? assignedTo)
    {
        using var context = _factory.CreateContext();
        var task = new HouseholdTask
        {
            Title = "Deleted task",
            Priority = TaskPriority.Low,
            Category = TaskCategory.General,
            AssignedToUserId = assignedTo,
            IsDeleted = true,
            IsActive = false,
            DeletedBy = deletedBy,
            LastModifiedBy = deletedBy
        };
        context.HouseholdTasks.Add(task);
        await context.SaveChangesAsync();
        return task;
    }

    public void Dispose() => _factory.Dispose();
}
