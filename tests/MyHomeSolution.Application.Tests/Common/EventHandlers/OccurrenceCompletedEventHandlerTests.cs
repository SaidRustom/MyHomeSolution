using FluentAssertions;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class OccurrenceCompletedEventHandlerTests
{
    private readonly IRealtimeNotificationService _notificationService =
        Substitute.For<IRealtimeNotificationService>();

    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public OccurrenceCompletedEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(
            new DateTimeOffset(2025, 7, 10, 16, 45, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldSendOccurrenceNotification_WithCorrectData()
    {
        var handler = new OccurrenceCompletedEventHandler(_notificationService, _dateTimeProvider);
        var occurrenceId = Guid.CreateVersion7();
        var taskId = Guid.CreateVersion7();
        var @event = new OccurrenceCompletedEvent(occurrenceId, taskId, "user-42");

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendOccurrenceNotificationAsync(
            Arg.Is<OccurrenceNotification>(n =>
                n.EventType == nameof(OccurrenceCompletedEvent) &&
                n.OccurrenceId == occurrenceId &&
                n.TaskId == taskId &&
                n.CompletedByUserId == "user-42" &&
                n.OccurredAt == new DateTimeOffset(2025, 7, 10, 16, 45, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldSendNullCompletedByUserId_WhenNotProvided()
    {
        var handler = new OccurrenceCompletedEventHandler(_notificationService, _dateTimeProvider);
        var @event = new OccurrenceCompletedEvent(Guid.CreateVersion7(), Guid.CreateVersion7(), null);

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendOccurrenceNotificationAsync(
            Arg.Is<OccurrenceNotification>(n => n.CompletedByUserId == null),
            Arg.Any<CancellationToken>());
    }
}
