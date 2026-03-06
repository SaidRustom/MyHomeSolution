using FluentAssertions;
using MyHomeSolution.Application.Common.EventHandlers;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Application.Common.Models;
using NSubstitute;

namespace MyHomeSolution.Application.Tests.Common.EventHandlers;

public sealed class OccurrenceSkippedEventHandlerTests
{
    private readonly IRealtimeNotificationService _notificationService =
        Substitute.For<IRealtimeNotificationService>();

    private readonly IDateTimeProvider _dateTimeProvider =
        Substitute.For<IDateTimeProvider>();

    public OccurrenceSkippedEventHandlerTests()
    {
        _dateTimeProvider.UtcNow.Returns(
            new DateTimeOffset(2025, 7, 11, 9, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public async Task Handle_ShouldSendOccurrenceNotification_WithCorrectData()
    {
        var handler = new OccurrenceSkippedEventHandler(_notificationService, _dateTimeProvider);
        var occurrenceId = Guid.CreateVersion7();
        var taskId = Guid.CreateVersion7();
        var @event = new OccurrenceSkippedEvent(occurrenceId, taskId);

        await handler.Handle(@event, CancellationToken.None);

        await _notificationService.Received(1).SendOccurrenceNotificationAsync(
            Arg.Is<OccurrenceNotification>(n =>
                n.EventType == nameof(OccurrenceSkippedEvent) &&
                n.OccurrenceId == occurrenceId &&
                n.TaskId == taskId &&
                n.CompletedByUserId == null &&
                n.OccurredAt == new DateTimeOffset(2025, 7, 11, 9, 0, 0, TimeSpan.Zero)),
            Arg.Any<CancellationToken>());
    }
}
