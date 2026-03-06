using FluentAssertions;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class TaskUpdatedEventHandlerTests
{
    private readonly IRealtimeNotificationService _notificationService =
        Substitute.For<IRealtimeNotificationService>();

    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public TaskUpdatedEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(
            new DateTimeOffset(2025, 6, 1, 14, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldSendTaskNotification_WithCorrectData()
    {
        var handler = new TaskUpdatedEventHandler(_notificationService, _dateTimeProvider);
        var @event = new TaskUpdatedEvent(Guid.CreateVersion7(), "Updated title");

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendTaskNotificationAsync(
            Arg.Is<TaskNotification>(n =>
                n.EventType == nameof(TaskUpdatedEvent) &&
                n.TaskId == @event.TaskId &&
                n.Title == "Updated title" &&
                n.OccurredAt == new DateTimeOffset(2025, 6, 1, 14, 30, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }
}
