using FluentAssertions;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class TaskCreatedEventHandlerTests
{
    private readonly IRealtimeNotificationService _notificationService =
        Substitute.For<IRealtimeNotificationService>();

    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public TaskCreatedEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(
            new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldSendTaskNotification_WithCorrectData()
    {
        var handler = new TaskCreatedEventHandler(_notificationService, _dateTimeProvider);
        var @event = new TaskCreatedEvent(Guid.CreateVersion7(), "Clean kitchen");

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendTaskNotificationAsync(
            Arg.Is<TaskNotification>(n =>
                n.EventType == nameof(TaskCreatedEvent) &&
                n.TaskId == @event.TaskId &&
                n.Title == "Clean kitchen" &&
                n.OccurredAt == new DateTimeOffset(2025, 6, 1, 12, 0, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldPassCancellationToken()
    {
        var handler = new TaskCreatedEventHandler(_notificationService, _dateTimeProvider);
        using var cts = new CancellationTokenSource();
        var token = cts.Token;

        await handler.Handle(new TaskCreatedEvent(Guid.CreateVersion7(), "Task"), token);

        await _notificationService.Received(1).SendTaskNotificationAsync(
            Arg.Any<TaskNotification>(), token);
    }
}
