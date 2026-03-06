using MediatR;
using Microsoft.EntityFrameworkCore;
using MyHomeSolution.Application.Common.Events;
using MyHomeSolution.Application.Common.Exceptions;
using MyHomeSolution.Application.Common.Interfaces;
using MyHomeSolution.Domain.Entities;
using MyHomeSolution.Domain.Enums;

namespace MyHomeSolution.Application.Features.Occurrences.Commands.CompleteOccurrence;

public sealed class CompleteOccurrenceCommandHandler(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUserService,
    IDateTimeProvider dateTimeProvider,
    IPublisher publisher)
    : IRequestHandler<CompleteOccurrenceCommand>
{
    public async Task Handle(CompleteOccurrenceCommand request, CancellationToken cancellationToken)
    {
        var occurrence = await dbContext.TaskOccurrences
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId && !o.IsDeleted, cancellationToken)
            ?? throw new NotFoundException(nameof(TaskOccurrence), request.OccurrenceId);

        occurrence.Status = OccurrenceStatus.Completed;
        occurrence.CompletedAt = dateTimeProvider.UtcNow;
        occurrence.CompletedByUserId = currentUserService.UserId;
        occurrence.Notes = request.Notes;

        await dbContext.SaveChangesAsync(cancellationToken);

        await publisher.Publish(
            new OccurrenceCompletedEvent(occurrence.Id, occurrence.HouseholdTaskId, currentUserService.UserId),
            cancellationToken);
    }
}
