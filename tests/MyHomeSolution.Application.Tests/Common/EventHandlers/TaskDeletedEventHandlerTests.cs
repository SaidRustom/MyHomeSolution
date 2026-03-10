using FluentAssertions;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class TaskDeletedEventHandlerTests
{
    private readonly IRealtimeNotificationService _notificationService =
        Substitute.For<IRealtimeNotificationService>();

    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public TaskDeletedEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(
            new DateTimeOffset(2025, 6, 2, 8, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldSendTaskNotification_WithTitle()
    {
        var handler = new TaskDeletedEventHandler(_notificationService, _dateTimeProvider);
        var taskId = Guid.CreateVersion7();
        var @event = new TaskDeletedEvent(taskId, "Test Task", 0, 0, []);

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendTaskNotificationAsync(
            Arg.Is<TaskNotification>(n =>
                n.EventType == nameof(TaskDeletedEvent) &&
                n.TaskId == taskId &&
                n.Title == "Test Task" &&
                n.OccurredAt == new DateTimeOffset(2025, 6, 2, 8, 0, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }
}
